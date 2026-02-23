#!/bin/bash
cd Console
echo "Compiling test..."
dotnet build -c Debug > /dev/null 2>&1

echo "Running logging test..."
echo ""
dotnet script TestLoggingProgram.cs --no-cache 2>&1 || {
    echo "dotnet script not available, using direct compilation..."
    # Compile and run as a regular program
    csc_temp=$(mktemp -d)
    dotnet publish -c Debug -o "$csc_temp" > /dev/null 2>&1
    
    cat > "$csc_temp/test.csproj" <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Application/Application.csproj" />
    <ProjectReference Include="../Infrastructure/Infrastructure.csproj" />
    <ProjectReference Include="../Logger/Logger.csproj" />
    <ProjectReference Include="../Model/Model.csproj" />
    <ProjectReference Include="../Shared/Shared.csproj" />
  </ItemGroup>
</Project>
CSPROJ
    
    cp TestLoggingProgram.cs "$csc_temp/Program.cs"
    cd "$csc_temp"
    dotnet run
    cd -
    rm -rf "$csc_temp"
}
