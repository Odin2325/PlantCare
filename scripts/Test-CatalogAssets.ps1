[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$entries=@(Get-Content (Join-Path $root 'deploy/catalog.houseplants.json') -Raw|ConvertFrom-Json)
$photos=@(Get-Content (Join-Path $root 'deploy/catalog-images.json') -Raw|ConvertFrom-Json)
if($entries.Count -lt 20){throw 'Reviewed catalog entries are missing.'}
if(@($photos.path|Sort-Object -Unique).Count -ne $photos.Count){throw 'Duplicate catalog image paths.'}
$referenced=@(foreach($entry in $entries){
    $entry.catalogProfile.photo
    foreach($reference in $entry.catalogProfile.growthReferences){$reference.photo}
})
if(@($referenced.path|Sort-Object -Unique).Count -ne $photos.Count){throw 'Catalog/image manifest count mismatch.'}
foreach($photo in $referenced){
    if($photo.path -notmatch '^/images/plants/[a-z0-9-]+\.(jpg|jpeg|png|webp)$'){throw 'Unexpected catalog image path.'}
    $manifest=@($photos|Where-Object path -eq $photo.path)
    if($manifest.Count -ne 1){throw 'Missing/ambiguous image attribution.'}
    foreach($field in @('credit','sourceUrl','licenseName','licenseUrl')){
        if($photo.$field -cne $manifest[0].$field){throw "Image attribution mismatch: $field"}
    }
    if($photo.title -cne $manifest[0].caption){throw 'Original image title was not retained.'}
    $source=[Uri]$photo.sourceUrl
    if($source.Scheme -ne 'https' -or $source.Query -or $source.UserInfo){throw 'Image source must be a permanent HTTPS page.'}
    $path=Join-Path (Join-Path $root 'plant-care-web/public') $photo.path.TrimStart('/')
    $file=Get-Item -LiteralPath $path
    if($file.Length -ne $manifest[0].bytes -or $file.Length -gt 1MB){throw 'Image file size mismatch/budget exceeded.'}
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $manifest[0].sha256){throw 'Image checksum mismatch.'}
}
Write-Output "PASS: $($photos.Count) original photo files, titles, credits, licenses and checksums match all catalog references."
