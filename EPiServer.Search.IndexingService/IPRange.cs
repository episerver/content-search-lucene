using System;
using System.Net.Sockets;
using System.Net;

namespace EPiServer.Search.IndexingService
{
    internal class IPRange
    {
        AddressFamily _addressFamily;
        byte[] _addressBytes;
        byte[] _maskBytes;

        public IPRange(AddressFamily addressFamily, byte[] addressBytes, byte[] maskBytes)
        {
            _addressFamily = addressFamily;
            _addressBytes = addressBytes;
            _maskBytes = maskBytes;
        }


        public bool IsInRange(IPAddress clientAddress)
        {
            if (_addressFamily != clientAddress.AddressFamily)
                return false;

            byte[] clientAddressBytes = clientAddress.GetAddressBytes();
            // Ok, check octet by octet...
            for (int i = 0; i < _addressBytes.Length; i++)
            {
                if ((_addressBytes[i] & _maskBytes[i]) != (clientAddressBytes[i] & _maskBytes[i]))
                    return false;
            }
            // All addressBytes ANDed with maskBytes matched with the client address
            return true;
        }

        public static bool TryParse(AddressFamily addressFamily, string value, out IPRange result)
        {
            int addressBits;
            byte[] addressBytes = null;
            byte[] maskBytes = null;

            result = null;

            if (addressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                addressBits = 32;
            }
            else if (addressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                addressBits = 128;
            }
            else
                return false;

            if (string.IsNullOrEmpty(value))
                return false;

            // Try to parse either of 127.0.0.1, 127.0.0.1/8 and 127.0.0.1/255.0.0.0 (IPv4)
            // or ::1, ::1/128, or ::1/::1 (yeah, it just happened...)
            System.Collections.BitArray ba = new System.Collections.BitArray(addressBits, true);

            int slashPosition = value.IndexOf('/');
            if (slashPosition >= 0)
            {
                int prefixLength = addressBits;
                string mask = value.Substring(slashPosition + 1);
                value = value.Substring(0, slashPosition);
                if (Int32.TryParse(mask, out prefixLength))
                {
                    if (prefixLength >= 0 && prefixLength <= addressBits)
                    {
                        for (int i = prefixLength; i < addressBits; i++)
                        {
                            ba.Set(i, false);
                        }
                    }
                }
                else
                {
                    // Failed to parse data after slash as an integer, try as an address instead
                    System.Net.IPAddress maskAddr;
                    if (System.Net.IPAddress.TryParse(mask, out maskAddr))
                    {
                        ba = new System.Collections.BitArray(maskAddr.GetAddressBytes());
                    }
                }
            }

            System.Net.IPAddress ip;
            if (System.Net.IPAddress.TryParse(value, out ip))
            {

                addressBytes = ip.GetAddressBytes();
                maskBytes = new byte[addressBytes.Length];
                ba.CopyTo(maskBytes, 0);

                result = new IPRange(addressFamily, addressBytes, maskBytes);
                return true;
            }
            return false;
        }
    }
}
