#Get-ADComputer -Filter * -Properties ServicePrincipalName | ? {$_.DistinguishedName -like }| Select-Object -ExpandProperty ServicePrincipalName

function Get-AdSPN {
    param (
        [String]$domain,
        [String]$adComputer
    )
    function Get-DN {
        param (
            [String]$DomainFullyQualifiedDomainName
        )
        $DomainFullyQualifiedDomainNameArray = $DomainFullyQualifiedDomainName -Split("\.")
        [int]$DomainNameFECount = 0
        ForEach ($DomainFullyQualifiedDomainNameArrayItem in $DomainFullyQualifiedDomainNameArray)
        { 
            IF ($DomainNameFECount -eq 0)
                { [string]$ADObjectDNArrayItemDomainName += "DC=" +$DomainFullyQualifiedDomainNameArrayItem }
            ELSE 
                { [string]$ADObjectDNArrayItemDomainName += ",DC=" +$DomainFullyQualifiedDomainNameArrayItem }
        $DomainNameFECount++
        }
        return $ADObjectDNArrayItemDomainName
    }
    
    $pattern = "CN=$adComputer,CN=*," + (Get-DN -DomainFullyQualifiedDomainName "$domain")
    $spn = Get-ADComputer -Filter * -Properties ServicePrincipalName | 
        ? {$_.DistinguishedName -like $pattern } |
            Select-Object -ExpandProperty ServicePrincipalName
    return $spn
}

Get-AdSPN -domain "gh0st.com" -adComputer "*"