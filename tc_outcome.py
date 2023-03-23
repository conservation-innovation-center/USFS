import pandas as pd
from pathlib import Path
from timeit import default_timer as timer
import datetime
import os
from shutil import copyfile
import sys
import subprocess as sp

# part 1 imports
# import arcpy
# from arcpy import env
# from arcpy.sa import *

# part 2 imports
import geopandas as gpd


def run_command(cmd_list):
    """
    Subprocess wrapper to execute other OS processes and waits for it to finish

    Args:
        cmd_list (list): list of args e.g. ['powershell.exe', 'copyitem', '"c:\abc.txt"', '-destination', '"d:\"']

    Returns:
        Subprocess CompletedProcess class: Includes information about state of process, stdout, etc.,
    """
    process = sp.run(cmd_list, check=True) # , capture_output=True)
    return process

class County_Tab:
    folder, env_folder, log_path, zone_path, out_table, cf, zone_path, t2_ras_p, zone = "", "", "", "", "", "", "", "", ""
    copy_list = []
    st_time = ''
    run_flag, statusFlag = True, True

    def __init__(self, folder, env_folder, log_path, cf, zone_path, dates_df, zone):
        if arcpy.CheckExtension("Spatial") == "Available":
            arcpy.CheckOutExtension("Spatial")
        self.folder = folder
        self.env_folder = env_folder
        self.log_path = log_path
        self.cf = cf
        self.zone_path = zone_path
        self.st_time = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        self.zone = zone
        t1_year = dates_df.loc[cf, 'T1']
        t2_year = dates_df.loc[cf, 'T2']
        in_ras = f'{self.folder}/{self.cf}/output/{self.cf}_landusechange_{t1_year}{t2_year}.tif'
        self.out_table = f'{self.env_folder}/{self.cf}_{zone}_ta.dbf'
        if os.path.isfile(self.out_table):
            self.run_flag = False
        fips = cf.split('_')[-1]
        self.t2_ras_p = f"{env_folder}/{cf}_landusechange_{t1_year}{t2_year}.tif"
        self.copy_list = ['powershell.exe', 'copy-item', in_ras, '-destination', self.t2_ras_p]
        self.statusFlag = True

    def run_tabulations(self):
        try:
            # copy from planimetrics to local drive
            run_command(self.copy_list)

            # update environment variables
            env.workspace = str(self.env_folder)
            arcpy.env.mask = self.t2_ras_p

            # tabulate area on change
            TabulateArea(self.zone_path, 'gridcode', self.t2_ras_p, 'VALUE', self.out_table, processing_cell_size=1) 

            # delete local copy of original change
            arcpy.Delete_management(self.t2_ras_p)

            end_time = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            # write to log
            with open(self.log_path, 'a') as dst:
                dst.write(f"{self.cf},complete, ,{self.st_time},{end_time}\n")
        except Exception as e:
            print(f'**************{self.cf} FAILED\n{e}')
            if os.path.isfile(self.t2_ras_p): # clean folder if a county fails
                arcpy.Delete_management(self.t2_ras_p)
            self.statusFlag = False
            end_time = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            # write to log
            with open(self.log_path, 'a') as dst:
                dst.write(f"{self.cf},failed,{e},{self.st_time},{end_time}\n")

class CreateIndicator:
    """
    1. for the zone, read and aggregate tables on shared ID
    2. mine VALUE_#### to calculate TC indicator:
        a.	TC gained over developed (from ROAD, IMPS, IMPO, TURF, PDEV to TCIS, TCTG, FORE, TCOT)
        b.	TC lost to developed (from TCIS, TCTG, FORE, TCOT  to TURF, PDEV, ROAD, IMPS, IMPO)
        c.	TC indicator = a - b
    2. join with shapefile and write results
    """
    cw_df = pd.DataFrame()
    output_folder, zone = "", ""
    tc_classes = ['TCIS', 'TCTG', 'FORE', 'TCOT']
    dev_classes = ['ROAD', 'IMPS', 'IMPO', 'TURF', 'PDEV']
    tc_to_dev, dev_to_tc = [], []
    zone_path = ""

    def __init__(self, cw_df, output_folder, zone, zone_path):
        self.cw_df = cw_df
        self.output_folder = output_folder
        self.tc_classes = self.tc_classes
        self.dev_classes = self.dev_classes
        self.zone = zone
        self.zone_path = zone_path

    def get_values(self):
        df = self.cw_df
        tc_vals = df[df['GenAbbrev'].isin(self.tc_classes)]['Value']
        dev_vals = df[df['GenAbbrev'].isin(self.dev_classes)]['Value']
        self.tc_to_dev = [f"{tc}{dev}" for tc in tc_vals for dev in dev_vals]
        self.dev_to_tc = [f"{dev}{tc}" for dev in dev_vals for tc in tc_vals]

    def agg_tables(self):
        # get list of values
        self.get_values()

        # get list of tables for zone
        tables = [x for x in os.listdir(f"{self.output_folder}/TA_results") if self.zone in x and x[-4:] == '.dbf']    
        df_list = [] # list of dfs containing GRIDCODE and metric
        for t in tables:
            # read in county table
            gdf = gpd.read_file(f"{self.output_folder}/TA_results/{t}")
            gdf.drop(columns=['geometry'], inplace=True)
            # select transitions of interest that exist in the table
            cols = [x for x in list(gdf) if x != 'GRIDCODE']
            gain = [f"VALUE_{x}" for x in self.dev_to_tc if f"VALUE_{x}" in cols]
            loss = [f"VALUE_{x}" for x in self.tc_to_dev if f"VALUE_{x}" in cols]
            # calculate net change
            gdf.loc[:, 'TCD'] = (gdf[gain].sum(axis=1) - gdf[loss].sum(axis=1)) / 4046.86
            # add to list
            df_list.append(gdf[['GRIDCODE', 'TCD']].copy())
            del gdf

        # concat all dfs together and aggregate on GRIDCODE
        df = pd.concat(df_list)
        del df_list
        df = df.groupby('GRIDCODE').sum().reset_index()

        # write results
        out_name = f"{self.output_folder}/TC_Outcome_{zone}"
        df.to_csv(f"{out_name}.csv", index=False)

        # read in zones, merge with table, and write results
        gdf = gpd.read_file(self.zone_path)
        gdf = gdf[['gridcode', 'geometry']]
        gdf.rename(columns={'gridcode':'GRIDCODE'}, inplace=True)
        gdf = gdf.merge(df, on='GRIDCODE')
        del df
        gdf.to_file(f"{out_name}.shp")



##################
if __name__ == '__main__':
    local_folder = f"C:/Users/smcdonald/Documents/Data/TC_outcome"
    input_folder = f"{local_folder}/input"
    output_folder = f"{local_folder}/output"

    # file paths
    folder = r"X:/landuse/version2"
    env_folder = f"{output_folder}/TA_results"
    zone_dict = {
        '100acrehex'  : 'CB_Region_Hex_100ac.shp',
        '1mihex'      : 'CB_Region_Hex_1mi2.shp'
    }

    # list either all LU  rasters or selectively by each county
    cfs = [x for x in os.listdir(folder) if '_' in x and os.path.isdir(f'{folder}/{x}')]

    # read in csv of cf and data years
    dates_df = pd.read_csv(f"{input_folder}/landcover_dates.csv")
    dates_df = dates_df.set_index('co_fips')

    # create log
    log_path = f"{output_folder}/chg_ta_log.csv"
    if not os.path.isfile(log_path):
        with open(log_path, 'w') as dst:
            dst.write(f"cf,status,note,starttime,endtime\n")

    failed_cofips = []

    # part 1: tabulate LULC change by zones
    # for zone in zone_dict:
    #     for cf in cfs:
    #         print('----------------------------------------')
    #         print(f'Starting {cf} for zone {zone}...')
    #         st = timer()

    #         # encapsulate data 
    #         dataObj = County_Tab(folder, env_folder, log_path, cf, f"{input_folder}/{zone_dict[zone]}", dates_df, zone)

    #         # county already tabulated
    #         if not dataObj.run_flag:
    #             continue
           
    #         # run tabulation
    #         dataObj.run_tabulations()

    #         # add failed cofips if it failed
    #         if not dataObj.statusFlag:
    #             failed_cofips.append(cf)
            
    #         # print time
    #         end = round((timer() - st)/60.0, 2)
    #         print(cf, end, 'minutes')

    # if len(failed_cofips) > 0:
    #     print("Counties Failed: ", failed_cofips)

    # part 2: aggregate tables and create metric
    cw_df = pd.read_csv(f"{input_folder}/land_use_color_table_FINAL.csv")
    for zone in zone_dict:
        print(f"Running {zone}...")
        ciObj = CreateIndicator(cw_df, output_folder, zone, f"{input_folder}/{zone_dict[zone]}")
        ciObj.agg_tables()
