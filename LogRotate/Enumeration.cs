using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace LogRotate
{
    /// <summary>
    /// http://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
    /// </summary>
    public abstract class Enumeration<T> : IComparable
        where T : Enumeration<T>
    {
        private readonly int _value;
        private readonly string _displayName;

        // ReSharper disable StaticFieldInGenericType
        private static int _nextValue;
        private readonly static HashSet<int> DefinedValues = new HashSet<int>();
        private readonly static HashSet<string> DefinedNames = new HashSet<string>();
        // ReSharper restore StaticFieldInGenericType

        protected Enumeration(string displayName)
        {
            if (!DefinedNames.Add(displayName))
            {
                throw new ArgumentException(String.Format(
                    "Display name {0} is already defined.", displayName));
            }

            var value = _nextValue;
            while (!DefinedValues.Add(value))
            {
                value++;
            }
            _nextValue = value + 1;

            _value = value;
            _displayName = displayName;
        }

        protected Enumeration(int value, string displayName)
        {
            if (!DefinedNames.Add(displayName))
            {
                throw new ArgumentException(String.Format(
                    "Display name {0} is already defined.", displayName));
            }
            if (!DefinedValues.Add(value))
            {
                throw new ArgumentException(String.Format(
                    "Value {0} is already defined [{1}]", value, displayName));
            }

            if (value == _nextValue)
            {
                _nextValue++;
            }

            _value = value;
            _displayName = displayName;
        }

        public int Value
        {
            get { return _value; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public static IEnumerable<T> GetAll()
        {
            var fields = typeof(T).GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => typeof(T).IsAssignableFrom(f.FieldType));
            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public static bool operator ==(Enumeration<T> fst, Enumeration<T> snd)
        {
            if (ReferenceEquals(fst, null) || ReferenceEquals(snd, null))
            {
                return ReferenceEquals(fst, snd);
            }
            return fst._value == snd._value;
        }

        public static bool operator !=(Enumeration<T> fst, Enumeration<T> snd)
        {
            return !(fst == snd);
        }

        public override bool Equals(object obj)
        {
            var otherValue = obj as Enumeration<T>;

            if (otherValue == null)
            {
                return false;
            }

            var typeMatches = GetType() == obj.GetType();
            var valueMatches = _value.Equals(otherValue.Value);

            return typeMatches && valueMatches;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static int AbsoluteDifference(Enumeration<T> firstValue, Enumeration<T> secondValue)
        {
            var absoluteDifference = Math.Abs(firstValue.Value - secondValue.Value);
            return absoluteDifference;
        }

        public static T FromValue(int value)
        {
            var matchingItem = Parse(value, "value", item => item.Value == value);
            return matchingItem;
        }

        public static T FromDisplayName(string displayName, bool caseSensitive = true)
        {
            var matchingItem = Parse(displayName, "display name", item => 
                String.Compare(item.DisplayName, displayName, caseSensitive, CultureInfo.InvariantCulture) == 0);
            return matchingItem;
        }

        private static T Parse<TK>(TK value, string description, Func<T, bool> predicate)
        {
            var matchingItem = GetAll().FirstOrDefault(predicate);

            if (matchingItem == null)
            {
                var message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(T));
                throw new ApplicationException(message);
            }

            return matchingItem;
        }

        public int CompareTo(object other)
        {
            return Value.CompareTo(((Enumeration<T>)other).Value);
        }
    }
    public class EnumerationTypeConverter<T> : TypeConverter
         where T : Enumeration<T>
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var str = value as string;
            if (str == null)
            {
                throw new Exception("Cannot convert null value.");
            }
            return Enumeration<T>.FromDisplayName(str);
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            var str = value as string;
            return str != null && Enumeration<T>.GetAll().Any(e => e.DisplayName == str);
        }
    }

}
