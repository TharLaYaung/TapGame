Add-Type -AssemblyName System.Drawing
$imagePath = 'd:\PopPulse\MobileGame\Assets\Resources\pop_pulse_logo.png'
$bitmap = [System.Drawing.Bitmap]::FromFile($imagePath)
$bitmap.MakeTransparent([System.Drawing.Color]::Black)
$bitmap.Save('d:\PopPulse\MobileGame\Assets\Resources\pop_pulse_logo_transparent.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
