if "%~1"=="" build Build
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe /t:%~1 build.proj

