function Invoke-RBCDElevatePrivilege {
    param (
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty()]
        [String]
        $UserName,
        [ValidateNotNullOrEmpty()]
        [SecureString]
        $Password,
        [ValidateNotNullOrEmpty()]
        [String]
        $LdapServer,
        [int16]
        $port = 389
    )

    $ldap = Connect-LDAP -User $UserName -Password $Password -Server $LdapServer -Port $port
    
    
    
}