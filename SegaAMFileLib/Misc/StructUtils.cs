using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Haruka.Arcade.SegaAMFileLib.Misc {

    /// <summary>
    /// Misc. methods to deal with structs and managed/unmanaged conversion.
    /// </summary>
    public class StructUtils {

        /// <summary>
        /// Checks if the given type has no fields. (= is an empty struct)
        /// </summary>
        /// <remarks>See https://stackoverflow.com/a/27851610 for more information.</remarks>
        /// <param name="t">The type to check.</param>
        /// <returns>true if the type has no fields, false if not.</returns>
        public static bool IsZeroSizeStruct(Type t) {
            return t.IsValueType && !t.IsPrimitive &&
                   t.GetFields((BindingFlags)0x34).All(fi => IsZeroSizeStruct(fi.FieldType));
        }

        /// <summary>
        /// Marshals the given struct to a raw byte array.
        /// </summary>
        /// <typeparam name="T">The type being converted.</typeparam>
        /// <param name="obj">The instance being converted.</param>
        /// <returns>A byte array which is a binary representation of the input object based on default C# marshalling, or an empty array if the given object is a zero-size struct.</returns>
        public static byte[] GetBytes<T>(T obj) {
            int size = Marshal.SizeOf(obj);

            if (size == 1 && IsZeroSizeStruct(obj.GetType())) {
                return new byte[0];
            }

            byte[] arr = new byte[size];

            GCHandle h = default;

            try {
                h = GCHandle.Alloc(arr, GCHandleType.Pinned);

                Marshal.StructureToPtr(obj, h.AddrOfPinnedObject(), false);
            } finally {
                if (h.IsAllocated) {
                    h.Free();
                }
            }

            return arr;
        }

        /// <summary>
        /// Unmarshals the given byte array to an object.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="arr">The object.</param>
        /// <returns>A struct based on the input array.</returns>
        public static T FromBytes<T>(byte[] arr) where T : struct {

            T str;

            GCHandle h = default;

            try {
                h = GCHandle.Alloc(arr, GCHandleType.Pinned);

                str = Marshal.PtrToStructure<T>(h.AddrOfPinnedObject());

            } finally {
                if (h.IsAllocated) {
                    h.Free();
                }
            }

            return str;
        }

        internal static unsafe void Copy(byte[] from, byte* to, int length) {
            ArgumentNullException.ThrowIfNull(from, nameof(from));
            fixed (byte* ptr = from) {
                Copy(ptr, 0, to, 0, length);
            }
        }

        internal static unsafe void Copy(byte* from, byte[] to, int length) {
            ArgumentNullException.ThrowIfNull(to, nameof(to));
            fixed (byte* ptr = to) {
                Copy(from, 0, ptr, 0, length);
            }
        }

        internal static unsafe void Copy(byte* from, int fromOffset, byte[] to, int toOffset, int length) {
            ArgumentNullException.ThrowIfNull(to, nameof(to));
            fixed (byte* ptr = to) {
                Copy(from, fromOffset, ptr, toOffset, length);
            }
        }

        internal static unsafe String Copy(byte* from, int length) {
            return new string((sbyte*)from, 0, length, Encoding.ASCII);
        }

        internal static unsafe void Copy(byte* from, int fromOffset, byte* to, int toOffset, int length) {
            for (int i = fromOffset, j = toOffset; i < fromOffset + length; i++, j++) {
                to[j] = from[i];
            }
        }
        
        internal static unsafe void Copy(String from, byte* to, int length) {
            Copy(from, to, length, Encoding.ASCII);
        }
        
        internal static unsafe void Copy(String from, byte* to, int length, Encoding encoding) {
            byte[] fromBytes = encoding.GetBytes(from);
            for (int i = 0; i < length && i < fromBytes.Length; i++) {
                to[i] = fromBytes[i];
            }
        }

        internal static unsafe String CopyToString(byte* from, int length) {
            return new String((sbyte*)from, 0, length);
        }
    }
}
