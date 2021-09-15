using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
namespace CheckBadThingsCSharp {
    class Program {
        public static void Main() {
            /*
            try {

                X509Certificate signer = X509Certificate.CreateFromSignedFile("D:\\chrome-download\\SteamSetup.exe");

                X509Certificate2 certificate = new X509Certificate2(signer);

                var certificateChain = new X509Chain {

                    ChainPolicy =

                    {

                        RevocationFlag = X509RevocationFlag.EntireChain,

                        RevocationMode = X509RevocationMode.Online,

                        UrlRetrievalTimeout = new TimeSpan(0, 1, 0),

                        VerificationFlags = X509VerificationFlags.NoFlag

                    }

                };

                var chainIsValid = certificate.Verify();


                if (chainIsValid) {
                    Console.WriteLine(certificate.SubjectName.Name);

                    Console.WriteLine(certificate.GetEffectiveDateString());

                    Console.WriteLine(certificate.GetExpirationDateString());

                    Console.WriteLine(certificate.Issuer);

                    return;
                }
                

                return ;

            } catch (Exception ex) {
                Console.WriteLine(ex.Message);

            }*/
            Console.WriteLine(CheckBadThingsCSharp.lib.VerifyFile.IsSigned("C:\\Users\\ellio\\appdata\\local\\microsoft\\onedrive\\onedrive.exe"));
        }

        static void PrintKeys(RegistryKey rkey) {

            // Retrieve all the subkeys for the specified key.
            string[] names = rkey.GetSubKeyNames();

            int icount = 0;

            Console.WriteLine("Subkeys of " + rkey.Name);
            Console.WriteLine("-----------------------------------------------");

            // Print the contents of the array to the console.
            foreach (string s in names) {
                Console.WriteLine(s);

                // The following code puts a limit on the number
                // of keys displayed.  Comment it out to print the
                // complete list.
                icount++;
                if (icount >= 10)
                    break;
            }
        }
    }
}
