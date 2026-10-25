function Get-CombatCardTouchBeforeGeometry([string]$Content) {
    $Content=$Content.Replace("`r`n","`n")
    if(-not $Content.Contains('public Vector2 contentOrigin;')){return $Content}
    $Content=$Content.Replace("            public Vector2 contentOrigin;`n",'')
    $block=@'
            if (touchScrollScopes.Count > 0)
            {
                // Unity applies GUI.matrix to the local point but adds enclosing
                // clip origins without scaling. Derive nested screen positions
                // from the parent's rendered content origin instead.
                Vector2 origin = touchScrollScopes.Peek().contentOrigin;
                p0 = origin + new Vector2(viewport.x * scaleX, viewport.y * scaleY);
                p1 = p0 + new Vector2(viewport.width * scaleX, viewport.height * scaleY);
            }
'@
    $block=$block.Replace("`r`n","`n")+"`n"
    if(-not $Content.Contains($block)){throw 'Nested viewport geometry patch was not found'}
    $Content=$Content.Replace($block,'')
    $block=@'
            Vector2 contentOrigin = p0 + new Vector2(
                (Mathf.Round(-native.x - viewRect.x) + correction.x) * scaleX,
                (Mathf.Round(-native.y - viewRect.y) + correction.y) * scaleY);
            touchScrollScopes.Push(new TouchScrollScope { identity = areaKey, visibleBounds = visible, contentOrigin = contentOrigin });
'@
    $block=$block.Replace("`r`n","`n")
    if(-not $Content.Contains($block)){throw 'Nested content origin patch was not found'}
    return $Content.Replace($block,'            touchScrollScopes.Push(new TouchScrollScope { identity = areaKey, visibleBounds = visible });')
}

function Write-CombatCardTouchExactSnapshot([string]$Content,[string]$Path,[string]$ExpectedSha) {
    $Content=$Content.Replace("`r`n","`n")
    foreach($newline in @("`n","`r`n")){
        foreach($bom in @($false,$true)){
            [IO.File]::WriteAllText($Path,$Content.Replace("`n",$newline),[Text.UTF8Encoding]::new($bom))
            if((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -eq $ExpectedSha){return}
        }
    }
    throw ('Reconstructed source does not match exact earlier SHA '+$ExpectedSha)
}
