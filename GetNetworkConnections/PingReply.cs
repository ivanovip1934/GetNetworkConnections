using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UPIpsecVPN
{
           

        /// <summary>Interoperability Helper
        ///     <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/bb309069(v=vs.85).aspx" />
        /// </summary>
        public static class Interop
        {
            private static IntPtr? icmpHandle;
            private static int? _replyStructLength;

            /// <summary>Returns the application legal icmp handle. Should be close by IcmpCloseHandle
            ///     <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/aa366045(v=vs.85).aspx" />
            /// </summary>
            public static IntPtr IcmpHandle {
                get {
                    if (icmpHandle == null) {
                        icmpHandle = IcmpCreateFile();
                        //TODO Close Icmp Handle appropiate
                    }

                    return icmpHandle.GetValueOrDefault();
                }
            }
            /// <summary>Returns the the marshaled size of the reply struct.</summary>
            public static int ReplyMarshalLength {
                get {
                    if (_replyStructLength == null) {
                        _replyStructLength = Marshal.SizeOf(typeof(Reply));
                    }
                    return _replyStructLength.GetValueOrDefault();
                }
            }


            [DllImport("Iphlpapi.dll", SetLastError = true)]
            private static extern IntPtr IcmpCreateFile();
            [DllImport("Iphlpapi.dll", SetLastError = true)]
            private static extern bool IcmpCloseHandle(IntPtr handle);
            [DllImport("Iphlpapi.dll", SetLastError = true)]
            public static extern uint IcmpSendEcho2Ex(IntPtr icmpHandle, IntPtr Event, IntPtr apcroutine, IntPtr apccontext, UInt32 sourceAddress, UInt32 destinationAddress, byte[] requestData, short requestSize, ref Option requestOptions, IntPtr replyBuffer, int replySize, int timeout);
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct Option
            {
                public byte Ttl;
                public readonly byte Tos;
                public byte Flags;
                public readonly byte OptionsSize;
                public readonly IntPtr OptionsData;
            }
                                    
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct Reply
            {
                public readonly UInt32 Address;
                public readonly int Status;
                public readonly int RoundTripTime;
                public readonly short DataSize;
                public readonly short Reserved;
                public readonly IntPtr DataPtr;
                public readonly Option Options;
            }
        }



          




        [Serializable]
        public class PingReplyExt
        {
            private readonly byte[] _buffer = null;
            private readonly IPAddress _ipAddress = null;
            private readonly uint _nativeCode = 0;
            private readonly TimeSpan _roundTripTime = TimeSpan.Zero;
            private readonly IPStatus _status = IPStatus.Unknown;
            private System.ComponentModel.Win32Exception _exception;


            public PingReplyExt(uint nativeCode, int replystatus, IPAddress ipAddress, TimeSpan duration) {
                _nativeCode = nativeCode;
                _ipAddress = ipAddress;
                if (Enum.IsDefined(typeof(IPStatus), replystatus))
                    _status = (IPStatus)replystatus;
            }
            public PingReplyExt(uint nativeCode, int replystatus, IPAddress ipAddress, int roundTripTime, byte[] buffer) {
                _nativeCode = nativeCode;
                _ipAddress = ipAddress;
                _roundTripTime = TimeSpan.FromMilliseconds(roundTripTime);
                _buffer = buffer;
                if (Enum.IsDefined(typeof(IPStatus), replystatus))
                    _status = (IPStatus)replystatus;
            }


            /// <summary>Native result from <code>IcmpSendEcho2Ex</code>.</summary>
            public uint NativeCode {
                get { return _nativeCode; }
            }
            public IPStatus Status {
                get { return _status; }
            }
            /// <summary>The source address of the reply.</summary>
            public IPAddress IpAddress {
                get { return _ipAddress; }
            }
            public byte[] Buffer {
                get { return _buffer; }
            }
            public TimeSpan RoundTripTime {
                get { return _roundTripTime; }
            }
            /// <summary>Resolves the <code>Win32Exception</code> from native code</summary>
            public Win32Exception Exception {
                get {
                    if (Status != IPStatus.Success)
                        return _exception ?? (_exception = new Win32Exception((int)NativeCode, Status.ToString()));
                    else
                        return null;
                }
            }

            public override string ToString() {
                if (Status == IPStatus.Success)
                    return Status + " from " + IpAddress + " in " + RoundTripTime + " ms with " + Buffer.Length + " bytes";
                else if (Status != IPStatus.Unknown)
                    return Status + " from " + IpAddress;
                else
                    return Exception.Message + " from " + IpAddress;
            }
        }
}
