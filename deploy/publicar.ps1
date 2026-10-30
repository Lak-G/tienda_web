<#
.SYNOPSIS
Publica TiendaNube desde Windows: publish linux-x64 self-contained, scp y reinicio del servicio por ssh.

.DESCRIPTION
Usa dotnet, tar.exe, scp y ssh, que vienen con Windows 10/11 (OpenSSH Client).
El EC2 no necesita SDK ni runtime de .NET.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File deploy\publicar.ps1 -Servidor 3.14.15.92 -Llave $HOME\.ssh\tiendanube.pem
#>
param(
    [Parameter(Mandatory)] [string] $Servidor,
    [string] $Llave = "$HOME\.ssh\tiendanube.pem",
    [string] $UsuarioSsh = 'ec2-user'
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$salida = Join-Path $raiz 'publish\linux-x64'
$paquete = Join-Path $raiz 'publish\tiendanube.tar.gz'
$destino = "$UsuarioSsh@$Servidor"

function Confirmar([string] $paso) {
    if ($LASTEXITCODE -ne 0) { throw "Falló: $paso (código $LASTEXITCODE)." }
}

if (Test-Path $salida) { Remove-Item -Recurse -Force $salida }
dotnet publish (Join-Path $raiz 'src\TiendaNube.Web') -c Release -r linux-x64 --self-contained -o $salida
Confirmar 'dotnet publish'

# Un solo archivo comprimido sube mucho más rápido que cientos de archivos con scp -r
tar -czf $paquete -C $salida .
Confirmar 'empaquetar con tar'

scp -i $Llave $paquete (Join-Path $raiz 'deploy\instalar-version.sh') "${destino}:/tmp/"
Confirmar 'scp al EC2'

ssh -i $Llave $destino 'sudo bash /tmp/instalar-version.sh /tmp/tiendanube.tar.gz'
Confirmar 'instalación y reinicio en el EC2'
