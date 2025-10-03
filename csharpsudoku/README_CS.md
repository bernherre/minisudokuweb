Instrucciones rápidas

Requisitos

Windows 10/11

.NET 8 SDK

Crear el proyecto

dotnet new winforms -n MiniSudoku -f net8.0-windows
cd MiniSudoku


Reemplaza el contenido de Program.cs con el código completo de abajo.

Compila y publica como .exe de un solo archivo

dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true


El ejecutable quedará en:

MiniSudoku\bin\Release\net8.0-windows\win-x64\publish\MiniSudoku.exe