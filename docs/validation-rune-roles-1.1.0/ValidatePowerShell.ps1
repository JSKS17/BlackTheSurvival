#requires -Version 7.0
$ErrorActionPreference='Stop'
$parsed=@()
foreach($file in Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1'){
 $tokens=$null;$errors=$null
 $null=[System.Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
 if($errors.Count){throw ($file.Name+': '+(($errors|ForEach-Object{$_.Message})-join '; '))}
 $parsed+=$file.Name
}
[ordered]@{result='PASS';parsed=$parsed} | ConvertTo-Json -Compress
