SET TOOL_PATH=(ToolPath)

IF NOT EXIST "%TOOL_PATH%\fake.exe" (
  rem #if (version == "latest")
  dotnet tool install fake-cli --tool-path ./%TOOL_PATH% --prerelease --allow-downgrade
  rem #else 
  dotnet tool install fake-cli --tool-path ./%TOOL_PATH% --version (version) --allow-downgrade
  rem #endif
)

"%TOOL_PATH%/fake.exe" %*
