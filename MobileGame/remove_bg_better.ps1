Add-Type -AssemblyName System.Drawing
$imgPath = 'D:\PopPulse\MobileGame\Assets\Resources\pop_pulse_logo.png'
$img = [System.Drawing.Bitmap]::FromFile($imgPath)
$out = New-Object System.Drawing.Bitmap($img.Width, $img.Height)
for ($y = 0; $y -lt $img.Height; $y++) {
    for ($x = 0; $x -lt $img.Width; $x++) {
        $p = $img.GetPixel($x, $y)
        $a = [Math]::Max($p.R, [Math]::Max($p.G, $p.B))
        if ($a -eq 0) {
            $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        } else {
            $r = [Math]::Min(255, [int]($p.R * 255 / $a))
            $g = [Math]::Min(255, [int]($p.G * 255 / $a))
            $b = [Math]::Min(255, [int]($p.B * 255 / $a))
            $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, $r, $g, $b))
        }
    }
}
$out.Save('D:\PopPulse\MobileGame\Assets\Resources\pop_pulse_logo_transparent.png', [System.Drawing.Imaging.ImageFormat]::Png)
$out.Dispose()
$img.Dispose()
