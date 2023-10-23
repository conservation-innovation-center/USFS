# Errors encountered and resolutions
```Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.```  
* Update the project to use .net 6.0 which has default of C# 10.
* edit ...\pdf508\pdf508\pdf508\pdf508.csproj  
`<TargetFramework>net6.0</TargetFramework>`

```session not created: This version of ChromeDriver only supports Chrome version 111
Current browser version is 114.0.5735.199 with binary path ...
```

* Install newer version from nuget: https://www.nuget.org/packages/Selenium.WebDriver.ChromeDriver/
* cd to project folder (should have bin folder) ...\pdf508\pdf508\pdf508
* Run command from nuget website