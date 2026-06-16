param(
    [string]$BHost = '127.0.0.1',
    [int]$Port = 9000,
    [int]$Seconds = 0,           # 0 = run until Ctrl+C
    [double]$Hz = 40.0,          # send rate; > 30 so a healthy stream shows PASS
    [string]$Mode = 'healthy'    # healthy | slow | frozen | stall  (to verify the bench's checks)
)

# M4 bench test sender: stream a fake skeleton to TrackingBench over UDP (no hardware, no Python).
# Run:  powershell -ExecutionPolicy Bypass -File "m4_test_stream.ps1" [-Mode healthy|slow|frozen|stall]
#   healthy : ~40 Hz, all joints jiggling, right hand waving  -> bench should read PASS
#   slow    : 20 Hz                                           -> bench should FAIL on delivery rate
#   frozen  : left foot held perfectly still (others move)    -> that sphere turns ORANGE, FAIL (occlusion)
#   stall   : streams ~6 s then stops sending                 -> spheres go grey, gap climbs, FAIL (dropout)

if ($Mode -eq 'slow') { $Hz = 20.0 }   # below the 30 Hz bar
$stallAfter = 6.0                       # 'stall' streams this long, then stops

# Raise the OS timer resolution to ~1 ms so Start-Sleep paces accurately (else PowerShell caps near ~20 Hz).
Add-Type -Name WinMM -Namespace Native -MemberDefinition '[System.Runtime.InteropServices.DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint p); [System.Runtime.InteropServices.DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint p);'
[Native.WinMM]::timeBeginPeriod(1) | Out-Null

$udp = New-Object System.Net.Sockets.UdpClient
$inv = [System.Globalization.CultureInfo]::InvariantCulture

# Joint order MUST match CollisionFeedback.Core.Joint:
# 0 Head, 1 Chest, 2 LeftHand, 3 RightHand, 4 LeftFoot, 5 RightFoot
$base = @(
    @(0.00, 1.70, 0.00),
    @(0.00, 1.30, 0.00),
    @(-0.20, 1.00, 0.00),
    @(0.20, 1.00, 0.00),
    @(-0.15, 0.05, 0.00),
    @(0.15, 0.05, 0.00)
)

$rand = New-Object System.Random
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$interval = 1.0 / $Hz
$next = 0.0
Write-Host ("Streaming '{0}' to {1}:{2} at ~{3:N0} Hz.  Ctrl+C to stop." -f $Mode, $BHost, $Port, $Hz)

try {
    while ($true) {
        $t = $sw.Elapsed.TotalSeconds
        if ($Seconds -gt 0 -and $t -ge $Seconds) { break }
        if ($Mode -eq 'stall' -and $t -ge $stallAfter) { Write-Host "stall: stopped sending (watch the bench gap climb)"; break }

        $vals = New-Object System.Collections.Generic.List[string]
        $vals.Add($t.ToString('F5', $inv))
        for ($j = 0; $j -lt 6; $j++) {
            $x = $base[$j][0]; $y = $base[$j][1]; $z = $base[$j][2]
            if ($j -eq 3) {                                       # wave the RightHand
                $x = 0.20 + 0.30 * [math]::Sin(2 * $t)
                $y = 1.00 + 0.20 * [math]::Sin(3 * $t)
            }
            $frozen = ($Mode -eq 'frozen' -and $j -eq 4)          # hold LeftFoot perfectly still -> FROZEN
            if (-not $frozen) {
                $x += ($rand.NextDouble() - 0.5) * 0.001          # +/- 0.5 mm noise so nothing else reads frozen
                $y += ($rand.NextDouble() - 0.5) * 0.001
                $z += ($rand.NextDouble() - 0.5) * 0.001
            }
            $vals.Add($x.ToString('F5', $inv))
            $vals.Add($y.ToString('F5', $inv))
            $vals.Add($z.ToString('F5', $inv))
        }
        $line = ($vals -join ',') + "`n"
        $bytes = [System.Text.Encoding]::ASCII.GetBytes($line)
        [void]$udp.Send($bytes, $bytes.Length, $BHost, $Port)

        $next += $interval
        $remaining = $next - $sw.Elapsed.TotalSeconds
        if ($remaining -gt 0.001) { Start-Sleep -Milliseconds ([int][math]::Round($remaining * 1000)) }
    }
}
finally {
    $udp.Close()
    [Native.WinMM]::timeEndPeriod(1) | Out-Null
    Write-Host "stopped."
}
