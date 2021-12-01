using System.Net;
using System.Net.Sockets;

namespace EPiServer.Search.IndexingService
{
    internal class IPRange
    {
        private readonly AddressFamily _addressFamily;
        private readonly byte[] _addressBytes;
        private readonly byte[] _maskBytes;

        public IPRange(AddressFamily addressFamily, byte[] addressBytes, byte[] maskBytes)
        {
            _addressFamily = addressFamily;
            _addressBytes = addressBytes;
            _maskBytes = maskBytes;
        }


        public bool IsInRange(IPAddress clientAddress)
        {
            if (_addressFamily != clientAddress.AddressFamily)
            {
                return false;
            }

            var clientAddressBytes = clientAddress.GetAddressBytes();
            // Ok, check octet by octet...
            for (var i = 0; i < _addressBytes.Length; i++)
            {
                if ((_addressBytes[i] & _maskBytes[i]) != (clientAddressBytes[i] & _maskBytes[i]))
                {
                    return false;
                }
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
            {
                return false;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Try to parse either of 127.0.0.1, 127.0.0.1/8 and 127.0.0.1/255.0.0.0 (IPv4)
            // or ::1, ::1/128, or ::1/::1 (yeah, it just happened...)
            var ba = new System.Collections.BitArray(addressBits, true);

            var slashPosition = value.IndexOf('/');
            if (slashPosition >= 0)
            {
                var prefixLength = addressBits;
                var mask = value.Substring(slashPosition + 1);
                value = value.Substring(0, slashPosition);
                if (int.TryParse(mask, out prefixLength))
                {
                    if (prefixLength >= 0 && prefixLength <= addressBits)
                    {
                        for (var i = prefixLength; i < addressBits; i++)
                        {
                            ba.Set(i, false);
                        }
                    }
                }
                else
                {
                    // Failed to parse data after slash as an integer, try as an address instead
                    if (System.Net.IPAddress.TryParse(mask, out var maskAddr))
                    {
                        ba = new System.Collections.BitArray(maskAddr.GetAddressBytes());
                    }
                }
            }

            if (System.Net.IPAddress.TryParse(value, out var ip))
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
