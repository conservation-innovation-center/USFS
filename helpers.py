import os
import pandas as pd

def MakeIterDir(parentDir, iterDir):
    """
    make a new folder in parentDir with the same name, indicated by iterDir, and the next iteration number
    """

    tnums = []
    for dir in os.listdir(parentDir):
        if iterDir in dir and len(dir) < 7:
            tnums.append(int(dir.replace(iterDir,"")))

    latestBN = f"{iterDir}{max(tnums)}"
    latestDir = os.path.join(parentDir,latestBN)
    latestDirNFiles = len([name for name in os.listdir(latestDir) if os.path.isfile(os.path.join(latestDir, name))])

    
    if latestDirNFiles == 0:
        # if the latest iteration directory is empty, set at iteration output directory
        LatestIterationDirectory = latestDir
        return LatestIterationDirectory
    else:
        #Otherwise create a new iteration directory
        NewIterDir = os.path.join(parentDir,f"{iterDir}{max(tnums)+1}")
        os.mkdir(NewIterDir)
        return NewIterDir

def set_years(cf):
    lu_tables = r"M:\code\landuse\lookup_tables"
    lc_dates = f"{lu_tables}/landcover_dates.csv" # T1 and T2 by cofips

    # read in years for county
    dates = pd.read_csv(lc_dates)
    dates = dates.set_index('co_fips')
    global T1_yr
    T1_yr = int(dates.loc[cf, 'T1'])
    global T2_yr
    T2_yr = int(dates.loc[cf, 'T2'])
    del dates
    return T1_yr, T2_yr


def gen_filepaths(cf, T2, coDir, hex_ac):
    # T1, T2 = set_years(cf)

    BayHexPath = f"M:/projects/data_processing/data_processing.gdb/baywide_{hex_ac}"
    CoHexPath = os.path.join(coDir,f"{cf}_hex.shp")
    calcCSVPath = os.path.join(coDir,f"{cf}_ta_calc.csv")
    ta_dbf_path = os.path.join(coDir,f"{cf}_hex_ta.dbf")
    ta_xlsx_path = ta_dbf_path.replace(".dbf",".xlsx")
    lulc_chgPath = f"B:/landuse/version2/{cf}/output/{cf}_landusechange_{T1}{T2}.tif"

    t2luPath = f"B:/landuse/version2/{cf}/output/{cf}_lu_{T2}.tif"
    t2_dbf_path = os.path.join(coDir,f"{cf}_hex_t2_ta.dbf")
    t2_xlsx_path = t2_dbf_path.replace(".dbf",".xlsx")
    t2_csv_path = t2_xlsx_path.replace('xlsx','csv')
    tajoined3Path = os.path.join(coDir,f"{cf}_hex_tajoined3.shp")

def test():
    print('fart')
    
