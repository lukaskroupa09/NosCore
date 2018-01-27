﻿

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNosCore.Core.Encryption
{
    public class WorldEncryption : EncryptionBase
    {
        #region Instantiation

        public WorldEncryption() : base(true)
        {
        }

        #endregion

        #region Methods

        private static string DecryptPrivate(string str)
        {
            List<byte> receiveData = new List<byte>();
            char[] table = { ' ', '-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'n' };
            int count;
            for (count = 0; count < str.Length; count++)
            {
                if (str[count] <= 0x7A)
                {
                    int len = str[count];

                    for (int i = 0; i < len; i++)
                    {
                        count++;

                        try
                        {
                            receiveData.Add(unchecked((byte)(str[count] ^ 0xFF)));
                        }
                        catch
                        {
                            receiveData.Add(255);
                        }
                    }
                }
                else
                {
                    int len = str[count];
                    len &= 0x7F;

                    for (int i = 0; i < len; i++)
                    {
                        count++;
                        int highbyte;
                        try
                        {
                            highbyte = str[count];
                        }
                        catch
                        {
                            highbyte = 0;
                        }
                        highbyte &= 0xF0;
                        highbyte >>= 0x4;

                        int lowbyte;
                        try
                        {
                            lowbyte = str[count];
                        }
                        catch
                        {
                            lowbyte = 0;
                        }
                        lowbyte &= 0x0F;

                        if (highbyte != 0x0 && highbyte != 0xF)
                        {
                            receiveData.Add(unchecked((byte)table[highbyte - 1]));
                            i++;
                        }

                        if (lowbyte != 0x0 && lowbyte != 0xF)
                        {
                            receiveData.Add(unchecked((byte)table[lowbyte - 1]));
                        }
                    }
                }
            }
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, receiveData.ToArray()));
        }

        public override string Decrypt(byte[] str, int sessionId = 0)
        {
            string encryptedString = "";
            int sessionKey = sessionId & 0xFF;
            byte sessionNumber = unchecked((byte)(sessionId >> 6));
            sessionNumber &= 0xFF;
            sessionNumber &= unchecked((byte)0x80000003);

            switch (sessionNumber)
            {
                case 0:
                    encryptedString = (from character in str let firstbyte = unchecked((byte)(sessionKey + 0x40)) select unchecked((byte)(character - firstbyte))).Aggregate(encryptedString,
                        (current, highbyte) => current + (char)highbyte);
                    break;

                case 1:
                    encryptedString = (from character in str let firstbyte = unchecked((byte)(sessionKey + 0x40)) select unchecked((byte)(character + firstbyte))).Aggregate(encryptedString,
                        (current, highbyte) => current + (char)highbyte);
                    break;

                case 2:
                    encryptedString =
                        (from character in str let firstbyte = unchecked((byte)(sessionKey + 0x40)) select unchecked((byte)(character - firstbyte ^ 0xC3))).Aggregate(encryptedString,
                            (current, highbyte) => current + (char)highbyte);
                    break;

                case 3:
                    encryptedString =
                        (from character in str let firstbyte = unchecked((byte)(sessionKey + 0x40)) select unchecked((byte)(character + firstbyte ^ 0xC3))).Aggregate(encryptedString,
                            (current, highbyte) => current + (char)highbyte);
                    break;

                default:
                    encryptedString += (char)0xF;
                    break;
            }

            string[] temp = encryptedString.Split((char)0xFF);
            StringBuilder save = new StringBuilder();

            for (int i = 0; i < temp.Length; i++)
            {
                save.Append(DecryptPrivate(temp[i]));
                if (i < temp.Length - 2)
                {
                    save.Append((char)0xFF);
                }
            }

            return save.ToString();
        }

        public override string DecryptCustomParameter(byte[] str)
        {
            try
            {
                StringBuilder encryptedStringBuilder = new StringBuilder();
                for (int i = 1; i < str.Length; i++)
                {
                    if (Convert.ToChar(str[i]) == 0xE)
                    {
                        return encryptedStringBuilder.ToString();
                    }

                    int firstbyte = Convert.ToInt32(str[i] - 0xF);
                    int secondbyte = firstbyte;
                    secondbyte &= 0xF0;
                    firstbyte = Convert.ToInt32(firstbyte - secondbyte);
                    secondbyte >>= 0x4;

                    switch (secondbyte)
                    {
                        case 0:
                        case 1:
                            encryptedStringBuilder.Append(' ');
                            break;

                        case 2:
                            encryptedStringBuilder.Append('-');
                            break;

                        case 3:
                            encryptedStringBuilder.Append('.');
                            break;

                        default:
                            secondbyte += 0x2C;
                            encryptedStringBuilder.Append(Convert.ToChar(secondbyte));
                            break;
                    }

                    switch (firstbyte)
                    {
                        case 0:
                            encryptedStringBuilder.Append(' ');
                            break;

                        case 1:
                            encryptedStringBuilder.Append(' ');
                            break;

                        case 2:
                            encryptedStringBuilder.Append('-');
                            break;

                        case 3:
                            encryptedStringBuilder.Append('.');
                            break;

                        default:
                            firstbyte += 0x2C;
                            encryptedStringBuilder.Append(Convert.ToChar(firstbyte));
                            break;
                    }
                }

                return encryptedStringBuilder.ToString();
            }
            catch (OverflowException)
            {
                return string.Empty;
            }
        }

        public override byte[] Encrypt(string str)
        {
            byte[] strBytes = Encoding.Default.GetBytes(str);
            int bytesLength = strBytes.Length;

            byte[] encryptedData = new byte[bytesLength + (int)Math.Ceiling((decimal)bytesLength / 0x7E) + 1];

            int j = 0;
            for (int i = 0; i < bytesLength; i++)
            {
                if ((i % 0x7E) == 0)
                {
                    encryptedData[i + j] = (byte)(bytesLength - i > 0x7E ? 0x7E : bytesLength - i);
                    j++;
                }
                encryptedData[i + j] = (byte)~strBytes[i];
            }
            encryptedData[encryptedData.Length - 1] = 0xFF;

            return encryptedData;
        }

        #endregion
    }
}