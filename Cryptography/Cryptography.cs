using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections;
using System.Collections.Generic;

//tietojen salausta
namespace Cryptography {

	//rsa:n julkiseen ja salaiseen avaimeen perustuva salaus
	public class RSA_Cryptaus {
		private CspParameters cp;
		private RSACryptoServiceProvider rsa;
		private RSAParameters RSAKeyInfo;

		public RSA_Cryptaus(string keyContainerName) {
			// Create the CspParameters object and set the key container 
			// name used to store the RSA key pair.
			cp = new CspParameters();
			cp.KeyContainerName = keyContainerName;
			
			// Create a new instance of RSACryptoServiceProvider that accesses
			// the key container MyKeyContainerName.
			rsa = new RSACryptoServiceProvider(cp);
			RSAKeyInfo=rsa.ExportParameters(false);
		}

		public ByteArray Encrypt(string data_to_encrypt) {
			return this.Encrypt(data_to_encrypt, this.RSAKeyInfo);
		}
		public ByteArray Encrypt(string data_to_encrypt, RSAParameters p_RSAKeyInfo) {
			try {
				//Create byte arrays to hold original and encrypted data. 
				ByteArray dataToEncrypt=new ByteArray();
				dataToEncrypt.Set_from_strData(data_to_encrypt);
				ByteArray encryptedData=new ByteArray();

				//Import the RSA Key information. This only needs 
				//toinclude the public key information.
				rsa.ImportParameters(p_RSAKeyInfo); //otetaan vastaan public key (pidä huoli ettei sisällä private key:a)

				//Encrypt the passed byte array and specify OAEP padding.   
				//OAEP padding is only available on Microsoft Windows XP or 
				//later.  
				encryptedData.bytearray = rsa.Encrypt(dataToEncrypt.bytearray, false);
				return encryptedData;
			} catch (CryptographicException e) {
				Debug.Log(e.Message);
				return new ByteArray();
			}
		}

		public string Decrypt(ByteArray dataToDecrypt) {
			return this.Decrypt(dataToDecrypt, this.RSAKeyInfo);
		}
		public string Decrypt(ByteArray dataToDecrypt, RSAParameters p_RSAKeyInfo) {
			try {
				ByteArray decryptedData=new ByteArray();

				//Import the RSA Key information. This only needs 
				//toinclude the public key information.
				rsa.ImportParameters(p_RSAKeyInfo); //otetaan vastaan public key (pidä huoli ettei sisällä private key:a)

				//Decrypt the passed byte array and specify OAEP padding.   
				//OAEP padding is only available on Microsoft Windows XP or 
				//later.
				decryptedData.bytearray = rsa.Decrypt(dataToDecrypt.bytearray, false);
				return decryptedData.Convert_to_string();
			} catch (CryptographicException e) {
				Debug.Log(e.Message);
				return "";
			}
		}

		public RSAParameters Generate_public_key() {
			//Generate a public/private key pair.
			RSACryptoServiceProvider rsa_temp = new RSACryptoServiceProvider();
			//Save the public key information to an RSAParameters structure.
			RSAParameters RSAKeyInfo = rsa_temp.ExportParameters(false);
			return RSAKeyInfo;
		}

		public void DeleteKeyFromContainer() {
			// Delete the key entry in the container.
			rsa.PersistKeyInCsp = false;
			
			// Call Clear to release resources and delete the key from the container.
			rsa.Clear();
		}
	}

	//tavukoodausta ja muunnokset
	public class ByteArray {
		UnicodeEncoding enc;
		public byte[] bytearray;

		public ByteArray() {
			enc = new UnicodeEncoding();
			//Encoding enc = new UTF8Encoding(true);
			//ASCIIEncoding enc = new ASCIIEncoding();
		}

		public string Convert_to_string() {
			return enc.GetString(bytearray);
		}

		//str -> tavukoodiksi
		public void Set_from_strData(string str_data) {
			try {
				bytearray = enc.GetBytes(str_data);
			} catch(Exception e) {
				Debug.Log(e.Message);
			}
		}

		//tavukoodiarvot pilkuilla erotettuina -> muunnetaan tavukoodiksi
		public void Set_from_strBytes(string str_bytes) {
			int i,j;
			List<byte> lista=new List<byte>();
			try {
				i=0; j=str_bytes.IndexOf(",", 1);
				while(j>0) {
					lista.Add(byte.Parse(str_bytes.Substring(i,j-i)));
					i=j+1; j=str_bytes.IndexOf(",", i+1);
				}
				//vielä viimeinen pätkä			
				if(str_bytes.Length>0) lista.Add(byte.Parse(str_bytes.Substring(i,str_bytes.Length-i)));
			} catch(Exception e) {
				Debug.Log(e.Message);
				lista.Add(0); //huolehditaan, että sisältää jotain
			}
			bytearray=new byte[lista.Count];
			lista.CopyTo(bytearray);
		}

		//muunnetaan tavukoodi merkkijonoesitykseksi, jossa arvot on pilkuilla erotettuina
		public override string ToString ()
		{
			string bytes="";
			if(bytearray.Length>0) bytes=bytearray[0].ToString();
			for(int i=1; i<bytearray.Length; i++)
				bytes= bytes + "," + bytearray[i];
			return bytes;
		}
	}
}
