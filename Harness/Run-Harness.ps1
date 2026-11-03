#requires -Version 7.0
[CmdletBinding()]
param(
 [ValidateSet('Both','Classic','Burgers')][string]$Chapter='Both',
 [switch]$RulesOnly,[switch]$SkipBuild,[switch]$CleanBuild,
 [ValidateRange(1024,65535)][int]$EditorPort=7800,
 [ValidateRange(60,300)][int]$TimeoutSeconds=120,
 [string]$UnityCli='unity'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runId=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$resultRoot=Join-Path $projectRoot 'HarnessResults'
$runRoot=Join-Path $resultRoot $runId
[IO.Directory]::CreateDirectory($runRoot)|Out-Null
$reports=[Collections.Generic.List[object]]::new()
$ownedProcesses=[Collections.Generic.List[Diagnostics.Process]]::new()
$failed=$false
function Write-JsonFile($Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))}
function Quote-CSharp([string]$Value){return '"'+$Value.Replace('\','\\').Replace('"','\"').Replace("`r",'\r').Replace("`n",'\n')+'"'}
function Invoke-Unity([string[]]$CommandArgs,[string]$LogName){
 $raw=(& $UnityCli @CommandArgs 2>&1|Out-String);$code=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $runRoot $LogName),$raw)
 try{$parsed=$raw|ConvertFrom-Json}catch{throw "Unity returned non-JSON output; see $LogName"}
 if($code -ne 0 -or -not $parsed.success){throw "Unity command failed; see $LogName : $($parsed.errors|ConvertTo-Json -Compress -Depth 8)"}
 if($null -ne $parsed.data -and $parsed.data.PSObject.Properties.Name -contains 'result'){
  $inner=$parsed.data.result
  if($null -ne $inner -and $inner.PSObject.Properties.Name -contains 'success' -and -not $inner.success){throw "Editor execution failed; see $LogName"}
 }
 return $parsed
}
function Wait-EditorReady{
 $deadline=[DateTime]::UtcNow.AddSeconds(60)
 do{
  $raw=(& $UnityCli status --json 2>&1|Out-String)
  try{$state=$raw|ConvertFrom-Json;$match=@($state.data.instances|Where-Object { $_.projectPath -eq $projectRoot -and $_.state -eq 'ready' -and $_.port -eq $EditorPort });if($match.Count -eq 1){return}}catch{}
  Start-Sleep -Milliseconds 750
 }while([DateTime]::UtcNow -lt $deadline)
 throw 'The project Editor did not become ready. Open this project, finish imports and check its Console.'
}
function Get-SourceHash{
 $digest=[Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA256)
 try{
  $files=@(Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -File;Get-ChildItem (Join-Path $projectRoot 'ProjectSettings') -File;Get-ChildItem (Join-Path $projectRoot 'Packages') -File)|Sort-Object FullName
  foreach($file in $files){$relative=[IO.Path]::GetRelativePath($projectRoot,$file.FullName).Replace('\','/');$digest.AppendData([Text.Encoding]::UTF8.GetBytes($relative));$digest.AppendData([IO.File]::ReadAllBytes($file.FullName))}
  return [Convert]::ToHexString($digest.GetHashAndReset())
 }finally{$digest.Dispose()}
}
function Read-SharedText([string]$Path){
 $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
 try{$reader=[IO.StreamReader]::new($stream);try{return $reader.ReadToEnd()}finally{$reader.Dispose()}}finally{$stream.Dispose()}
}
function Free-UdpPort{
 $udp=[Net.Sockets.UdpClient]::new([Net.IPEndPoint]::new([Net.IPAddress]::Loopback,0))
 try{return $udp.Client.LocalEndPoint.Port}finally{$udp.Dispose()}
}
function Start-Player([string]$Role,[string]$Scenario,[string]$Folder,[int]$Port){
 $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$script:binary;$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.WindowStyle=[Diagnostics.ProcessWindowStyle]::Hidden;$info.WorkingDirectory=$projectRoot
 $arguments=@('-batchmode','-nographics','-logFile',(Join-Path $Folder ($Role+'.player.log')),'--kitchen-harness','--harness-role',$Role,'--harness-dir',$Folder,'--harness-port',"$Port",'--harness-chapter',$Scenario,'--harness-timeout',"$TimeoutSeconds",'--harness-run-id',$runId)
 foreach($argument in $arguments){$info.ArgumentList.Add($argument)}
 $process=[Diagnostics.Process]::Start($info);$ownedProcesses.Add($process);return $process
}
function Invoke-Scenario([string]$Name){
 $folder=Join-Path $runRoot $Name.ToLowerInvariant();[IO.Directory]::CreateDirectory($folder)|Out-Null
 $port=Free-UdpPort;$players=[Collections.Generic.List[Diagnostics.Process]]::new();$overflowStarted=$false;$lastPhase='';$deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds+25)
 $hostPlayer=Start-Player 'host' $Name $folder $port;$players.Add($hostPlayer)
 try{
  while(-not (Test-Path (Join-Path $folder 'host-ready.signal'))){if($hostPlayer.HasExited -or [DateTime]::UtcNow -gt $deadline){throw "Host startup failed for $Name. See $folder"};Start-Sleep -Milliseconds 150}
  foreach($number in 1..3){$players.Add((Start-Player "client-$number" $Name $folder $port))}
  while($true){
   if(-not $overflowStarted -and (Test-Path (Join-Path $folder 'ready-for-overflow.signal'))){$players.Add((Start-Player 'overflow' $Name $folder $port));$overflowStarted=$true}
   $controlRecord=Get-ChildItem -LiteralPath $folder -Filter 'control-*.json' -File|Sort-Object Name|Select-Object -Last 1
   $controlPath=if($null -eq $controlRecord){Join-Path $folder 'control-0000.json'}else{$controlRecord.FullName}
   if(Test-Path $controlPath){try{$control=(Read-SharedText $controlPath)|ConvertFrom-Json;if($control.phase -ne $lastPhase){$lastPhase=$control.phase;Write-Host "[$Name] $lastPhase"}}catch{}}
   foreach($player in $players){if($player.HasExited -and $player.ExitCode -ne 0){throw "Integration player $($player.Id) exited with $($player.ExitCode) in $Name. See $folder"}}
   if($overflowStarted -and @($players|Where-Object {-not $_.HasExited}).Count -eq 0){break}
   if([DateTime]::UtcNow -gt $deadline){throw "Integration deadline exceeded in $Name, phase $lastPhase. See $folder"}
   Start-Sleep -Milliseconds 150
  }
  foreach($role in @('host','client-1','client-2','client-3','overflow')){
   $path=Join-Path $folder ($role+'.json');if(-not (Test-Path $path)){throw "Missing $role report in $Name"}
   $report=Get-Content $path -Raw|ConvertFrom-Json;
   if($report.schemaVersion -ne 1 -or $report.role -ne $role -or $report.chapter -ne $Name -or @($report.checks).Count -eq 0){throw "Invalid or empty structured report for $role in $Name"}
   $reports.Add($report)
   if($report.status -ne 'passed'){throw "$role failed in $Name : $($report.error)"}
  }
  Write-Host "[$Name] PASSED"
 }finally{
  foreach($player in $players){if(-not $player.HasExited){$player.Kill($true);$player.WaitForExit(5000)|Out-Null}}
  foreach($role in @('host','client-1','client-2','client-3','overflow')){
   if(@($reports|Where-Object {$_.role -eq $role -and $_.chapter -eq $Name}).Count -gt 0){continue}
   $path=Join-Path $folder ($role+'.json')
   try{if(-not (Test-Path $path)){throw 'No structured result; see player log and last control phase.'};$reports.Add((Get-Content $path -Raw|ConvertFrom-Json))}catch{$reports.Add([pscustomobject]@{role=$role;chapter=$Name;status='failed';error=$_.ToString();checks=@()})}
  }
 }
}
function Write-Summary{
 $checks=@($reports|ForEach-Object {$_.checks})
 $summary=[ordered]@{schemaVersion=1;runId=$runId;status=$(if($failed){'failed'}else{'passed'});project=$projectRoot;finishedUtc=[DateTime]::UtcNow.ToString('o');rulesOnly=[bool]$RulesOnly;chapters=$Chapter;reportCount=$reports.Count;checkCount=$checks.Count;reports=@($reports.ToArray())}
 Write-JsonFile (Join-Path $runRoot 'summary.json') $summary
 Write-JsonFile (Join-Path $resultRoot 'latest.json') @{runId=$runId;status=$summary.status;path=$runRoot}
 $settings=[Xml.XmlWriterSettings]::new();$settings.Indent=$true;$settings.Encoding=[Text.UTF8Encoding]::new($false)
 $writer=[Xml.XmlWriter]::Create((Join-Path $runRoot 'junit.xml'),$settings)
 try{
  $writer.WriteStartElement('testsuites')
  foreach($report in $reports){
   $writer.WriteStartElement('testsuite');$writer.WriteAttributeString('name',"$($report.chapter).$($report.role)");$caseCount=@($report.checks).Count;if($report.status -ne 'passed'){$caseCount++};$writer.WriteAttributeString('tests',"$caseCount")
   foreach($check in $report.checks){$writer.WriteStartElement('testcase');$writer.WriteAttributeString('name',$check.name);if($report.role -eq 'rules'){$writer.WriteAttributeString('time',([double]$check.elapsedSeconds).ToString('0.000',[Globalization.CultureInfo]::InvariantCulture))};if($check.status -ne 'passed'){$writer.WriteStartElement('failure');$writer.WriteString([string]$check.detail);$writer.WriteEndElement()};$writer.WriteEndElement()}
   if($report.status -ne 'passed'){$writer.WriteStartElement('testcase');$writer.WriteAttributeString('name','process-result');$writer.WriteStartElement('failure');$writer.WriteString([string]$report.error);$writer.WriteEndElement();$writer.WriteEndElement()}
   $writer.WriteEndElement()
  }
  $writer.WriteEndElement()
 }finally{$writer.Dispose()}
 Write-Host "Results: $runRoot"
}
Push-Location $projectRoot
try{
 Write-Host 'Compiling the current project...'
 Invoke-Unity @('recompile','--project-path',$projectRoot,'--timeout','180','--json') 'compile.json'|Out-Null
 Wait-EditorReady
 $rulesPath=Join-Path $runRoot 'rules.json';$code='return CookedOver.Editor.KitchenHarnessEditor.VerifyRules('+(Quote-CSharp $rulesPath)+').status;'
 Invoke-Unity @('command','script','eval',$code,'--port',"$EditorPort",'--timeout','120','--json') 'rules-cli.json'|Out-Null
 if(-not (Test-Path $rulesPath)){throw 'Rules report was not produced.'}
 $rules=Get-Content $rulesPath -Raw|ConvertFrom-Json;$reports.Add($rules);if($rules.status -ne 'passed'){throw 'Game rules or snapshot contracts failed.'}
 Write-Host 'Rules and snapshot contracts PASSED'
 if(-not $RulesOnly){
  $script:binary=Join-Path $projectRoot 'Builds/Harness/CookedOverHarness.exe';$stampPath=Join-Path $projectRoot 'Builds/Harness/build.json';$hash=Get-SourceHash
  $fresh=$false;if((Test-Path $script:binary) -and (Test-Path $stampPath)){$stamp=Get-Content $stampPath -Raw|ConvertFrom-Json;$fresh=$stamp.sourceHash -eq $hash -and $stamp.projectPath -eq $projectRoot}
  if($CleanBuild){$fresh=$false}
  if(-not $fresh){
   if($SkipBuild){throw 'Harness binary is missing or stale. Run again without -SkipBuild.'}
   Write-Host 'Building an isolated integration player...'
   Invoke-Unity @('command','script','eval',('return CookedOver.Editor.KitchenHarnessEditor.BuildPlayer('+$(if($CleanBuild){'true'}else{'false'})+');'),'--port',"$EditorPort",'--timeout','600','--json') 'build-cli.json'|Out-Null
   if(-not (Test-Path $script:binary)){throw 'Build completed without producing the expected executable.'}
   Write-JsonFile $stampPath @{sourceHash=(Get-SourceHash);builtUtc=[DateTime]::UtcNow.ToString('o');projectPath=$projectRoot;binaryPath=$script:binary}
   Wait-EditorReady
  }else{Write-Host 'Using a source-matched harness build.'}
  $chapters=if($Chapter -eq 'Both'){@('Classic','Burgers')}else{@($Chapter)}
  foreach($scenario in $chapters){Invoke-Scenario $scenario}
 }
}catch{
 $failed=$true;$message=$_.ToString();Write-Host "FAILED: $message" -ForegroundColor Red
 $reports.Add([pscustomobject]@{role='runner';chapter=$Chapter;status='failed';error=$message;checks=@()})
}finally{
 foreach($player in $ownedProcesses){try{if(-not $player.HasExited){$player.Kill($true);$player.WaitForExit(5000)|Out-Null};$player.Dispose()}catch{}}
 Write-Summary;Pop-Location
}
if($failed){exit 1}else{exit 0}





