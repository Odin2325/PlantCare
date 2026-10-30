[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$entries=@(Get-Content (Join-Path $root 'deploy/catalog.houseplants.json') -Raw|ConvertFrom-Json)
$photos=@(Get-Content (Join-Path $root 'deploy/catalog-images.json') -Raw|ConvertFrom-Json)
if($entries.Count -lt 43){throw 'Reviewed catalog entries are missing.'}
if(@($photos.path|Sort-Object -Unique).Count -ne $photos.Count){throw 'Duplicate catalog image paths.'}
$referenced=@(foreach($entry in $entries){
    if($entry.catalogProfile.photo){$entry.catalogProfile.photo}
    foreach($reference in $entry.catalogProfile.growthReferences){$reference.photo}
})
$coverage=@(Get-Content (Join-Path $root 'deploy/catalog-cultivar-photos.json') -Raw|ConvertFrom-Json)
$cultivars=@($entries|Where-Object {$_.catalogProfile.cultivarName})
if($coverage.Count -ne $cultivars.Count -or @($coverage.scientificName|Sort-Object -Unique).Count -ne $coverage.Count){throw 'Cultivar photo coverage is incomplete or duplicated.'}
$cultivarPaths=@(foreach($entry in $cultivars){
    $profile=$entry.catalogProfile
    $row=@($coverage|Where-Object scientificName -CEQ $entry.scientificName)
    if($row.Count -ne 1 -or $row[0].cultivarName -cne $profile.cultivarName){throw 'Cultivar photo coverage identity mismatch.'}
    if($profile.photoIsSpeciesReference){throw 'Use a pending placeholder instead of a generic cultivar photograph.'}
    if($profile.photo){
        if($row[0].status -cne 'verified' -or $row[0].photoPath -cne $profile.photo.path -or $row[0].sourceUrl -cne $profile.photo.sourceUrl){throw 'Cultivar image source was not reviewed.'}
        $profile.photo.path
    } elseif($row[0].status -cne 'pending' -or $row[0].photoPath -or $row[0].sourceUrl){throw 'Missing cultivar photographs must remain explicitly pending.'}
})
if(@($cultivarPaths|Sort-Object -Unique).Count -ne $cultivarPaths.Count){throw 'Cultivars must not share a photograph.'}
$generalPaths=@($entries|Where-Object {-not $_.catalogProfile.cultivarName}|ForEach-Object {$_.catalogProfile.photo.path})
if(@($cultivarPaths|Where-Object {$_ -in $generalPaths}).Count){throw 'Cultivar photograph is shared with a general species entry.'}
$cultivarAssets=@($photos|Where-Object {$_.path -in $cultivarPaths})
if(@($cultivarAssets.sha256|Sort-Object -Unique).Count -ne $cultivarPaths.Count -or @($cultivarAssets.sourceUrl|Sort-Object -Unique).Count -ne $cultivarPaths.Count){throw 'Cultivar photos must have distinct bytes and source files, not renamed duplicates.'}
$generalHashes=@($photos|Where-Object {$_.path -in $generalPaths}|ForEach-Object sha256)
if(@($cultivarAssets|Where-Object {$_.sha256 -in $generalHashes}).Count){throw 'A generic species photograph was copied under a cultivar filename.'}
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
Write-Output "PASS: $($photos.Count) photo assets, titles, credits, licenses and checksums match all references; $($cultivarPaths.Count) distinct reviewed cultivar photos and $($cultivars.Count-$cultivarPaths.Count) pending."
