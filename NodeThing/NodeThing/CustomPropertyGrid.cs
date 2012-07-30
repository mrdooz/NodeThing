// Original code by Ben Ratzlaff
// http://www.codeproject.com/Articles/5996/Populating-a-PropertyGrid-using-Reflection-Emit

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NodeThing
{
    class PersonConverter : ExpandableObjectConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type t)
        {
            if (t == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, t);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo info, object value) 
        {
      if (value is string) {
         try {
         string s = (string) value;
             return null;
/*
         // parse the format "Last, First (Age)"
         //
         int comma = s.IndexOf(',');
         if (comma != -1) {
            // now that we have the comma, get 
            // the last name.
            string last = s.Substring(0, comma);
             int paren = s.LastIndexOf('(');
            if (paren != -1 && 
                  s.LastIndexOf(')') == s.Length - 1) {
               // pick up the first name
               string first = s.Substring(comma + 1, paren - comma - 1);
               // get the age
               int age = Int32.Parse(s.Substring(paren + 1, s.Length - paren - 2));
                  Person p = new Person();
                  p.Age = age;
                  p.LastName = last.Trim();
                  p.FirstName = first.Trim();
                  return p;
            }
         }
 */
      }
      catch {}
      // if we got this far, complain that we
      // couldn't parse the string
      //
      throw new ArgumentException("Can not convert '" + (string)value + "' to type Person");
      }
      return base.ConvertFrom(context, info, value);
   }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
/*
            if (destType == typeof(string) && value is Person) {
                Person p = (Person)value;
                // simply build the string as "Last, First (Age)"
                return p.LastName + ", " + p.FirstName + " (" + p.Age.ToString() + ")";
            }
 */
            return base.ConvertTo(context, culture, value, destType);
        }
    }
    /// <summary>
    /// A property grid that dynamically generates a Type to conform to desired input
    /// </summary>
    public class CustomPropertyGrid : PropertyGrid
    {
        private Dictionary<string, Setting> _settings;
        private Hashtable _typeHash;

        public CustomPropertyGrid()
        {
            InstantUpdate = true;
            TypeName = "DefType";
            InitTypes();
        }

        [Description("Name of the type that will be internally created"), DefaultValue("DefType")]
        public string TypeName { get; set; }

        [DefaultValue(true), Description("If true, the Setting.Update() event will be called when a property changes")]
        public bool InstantUpdate { get; set; }

        [Browsable(false)]
        public Dictionary<string, Setting> Settings
        {
            set
            {
                _settings = value;
                // Reflection.Emit code below copied and modified from http://msdn.microsoft.com/en-us/library/system.reflection.emit.propertybuilder.aspx

                var myDomain = Thread.GetDomain();
                var myAsmName = new AssemblyName { Name = "TempAssembly" };

                var assemblyBuilder = myDomain.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");

                //create our type
                var newType = moduleBuilder.DefineType(TypeName, TypeAttributes.Public);

                //create the hashtable used to store property values
                var hashField = newType.DefineField("table", typeof(Hashtable), FieldAttributes.Private);
                CreateHashMethod(newType.DefineProperty("Hash", PropertyAttributes.None, typeof(Hashtable), new Type[] { }), newType, hashField);

                var h = new Hashtable();
                foreach (string key in _settings.Keys) {
                    var s = _settings[key];
                    h[key] = s.Value;
                    EmitProperty(newType, hashField, s, key);
                }

                // Create an object of the created type
                var myType = newType.CreateType();
                var ci = myType.GetConstructor(new Type[] { });
                if (ci != null) {
                    var o = ci.Invoke(new Object[] { });

                    //set the object's hashtable - in the future i would like to do this in the emitted object's constructor
                    var pi = myType.GetProperty("Hash");
                    pi.SetValue(o, h, null);

                    SelectedObject = o;
                }
            }
        }

        protected override void OnPropertyValueChanged(PropertyValueChangedEventArgs e)
        {
            base.OnPropertyValueChanged(e);
            var setting = _settings[e.ChangedItem.Label];
            setting.Value = e.ChangedItem.Value;

            if (InstantUpdate)
                setting.TriggerUpdate(e);
        }

        private void CreateHashMethod(PropertyBuilder propBuild, TypeBuilder typeBuild, FieldBuilder hash)
        {
            // First, we'll define the behavior of the "get" property for Hash as a method.
            var typeHashGet = typeBuild.DefineMethod("GetHash", MethodAttributes.Public, typeof(Hashtable), new Type[] { });
            var ilg = typeHashGet.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, hash);
            ilg.Emit(OpCodes.Ret);

            // Now, we'll define the behavior of the "set" property for Hash.
            var typeHashSet = typeBuild.DefineMethod("SetHash", MethodAttributes.Public, null, new[] { typeof(Hashtable) });
            ilg = typeHashSet.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Stfld, hash);
            ilg.Emit(OpCodes.Ret);

            // map the two methods created above to their property
            propBuild.SetGetMethod(typeHashGet);
            propBuild.SetSetMethod(typeHashSet);

            //add the [Browsable(false)] property to the Hash property so it doesnt show up on the property list
            var ci = typeof(BrowsableAttribute).GetConstructor(new[] { typeof(bool) });
            if (ci != null) {
                var cab = new CustomAttributeBuilder(ci, new object[] { false });
                propBuild.SetCustomAttribute(cab);
            }
        }

        /// <summary>
        /// Initialize a private hashtable with type-opCode pairs so i dont have to write a long if/else statement when outputting msil
        /// </summary>
        private void InitTypes()
        {
            _typeHash = new Hashtable();
            _typeHash[typeof(sbyte)] = OpCodes.Ldind_I1;
            _typeHash[typeof(byte)] = OpCodes.Ldind_U1;
            _typeHash[typeof(char)] = OpCodes.Ldind_U2;
            _typeHash[typeof(short)] = OpCodes.Ldind_I2;
            _typeHash[typeof(ushort)] = OpCodes.Ldind_U2;
            _typeHash[typeof(int)] = OpCodes.Ldind_I4;
            _typeHash[typeof(uint)] = OpCodes.Ldind_U4;
            _typeHash[typeof(long)] = OpCodes.Ldind_I8;
            _typeHash[typeof(ulong)] = OpCodes.Ldind_I8;
            _typeHash[typeof(bool)] = OpCodes.Ldind_I1;
            _typeHash[typeof(double)] = OpCodes.Ldind_R8;
            _typeHash[typeof(float)] = OpCodes.Ldind_R4;
        }

        /// <summary>
        /// emits a generic get/set property in which the result returned resides in a hashtable whos key is the name of the property
        /// </summary>
        private void EmitProperty(TypeBuilder tb, FieldBuilder hash, Setting setting, string name)
        {
            //to figure out what opcodes to emit, i would compile a small class having the functionality i wanted, and viewed it with ildasm.
            //peverify is also kinda nice to use to see what errors there are. 

            //define the property first
            var objType = setting.Value.GetType();
            var pb = tb.DefineProperty(name, PropertyAttributes.None, objType, new Type[] { });

            //now we define the get method for the property
            var getMethod = tb.DefineMethod("get_" + name, MethodAttributes.Public, objType, new Type[] { });
            var ilg = getMethod.GetILGenerator();
            ilg.DeclareLocal(objType);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, hash);
            ilg.Emit(OpCodes.Ldstr, name);

            ilg.EmitCall(OpCodes.Callvirt, typeof(Hashtable).GetMethod("get_Item"), null);
            if (objType.IsValueType) {
                ilg.Emit(OpCodes.Unbox, objType);
                if (_typeHash[objType] != null)
                    ilg.Emit((OpCode)_typeHash[objType]);
                else
                    ilg.Emit(OpCodes.Ldobj, objType);
            } else
                ilg.Emit(OpCodes.Castclass, objType);

            ilg.Emit(OpCodes.Stloc_0);
            ilg.Emit(OpCodes.Br_S, (byte)0);
            ilg.Emit(OpCodes.Ldloc_0);
            ilg.Emit(OpCodes.Ret);

            //now we generate the set method for the property
            var setMethod = tb.DefineMethod("set_" + name, MethodAttributes.Public, null, new[] { objType });
            ilg = setMethod.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, hash);
            ilg.Emit(OpCodes.Ldstr, name);
            ilg.Emit(OpCodes.Ldarg_1);
            if (objType.IsValueType)
                ilg.Emit(OpCodes.Box, objType);
            ilg.EmitCall(OpCodes.Callvirt, typeof(Hashtable).GetMethod("set_Item"), null);
            ilg.Emit(OpCodes.Ret);

            //put the get/set methods in with the property
            pb.SetGetMethod(getMethod);
            pb.SetSetMethod(setMethod);

            //if we specified a description, we will now add the DescriptionAttribute to our property
            if (setting.Description != null) {
                var ci = typeof(DescriptionAttribute).GetConstructor(new[] { typeof(string) });
                if (ci != null) {
                    var cab = new CustomAttributeBuilder(ci, new object[] { setting.Description });
                    pb.SetCustomAttribute(cab);
                }
            }

            // Add the type-converter
            var tc = typeof (TypeConverterAttribute).GetConstructor(new[] {typeof (Type)});
            var xx = new CustomAttributeBuilder(tc, new object[] {typeof (PersonConverter)});
            pb.SetCustomAttribute(xx);


            //add a CategoryAttribute if specified
            if (setting.Category != null) {
                var ci = typeof(CategoryAttribute).GetConstructor(new[] { typeof(string) });
                if (ci != null) {
                    var cab = new CustomAttributeBuilder(ci, new object[] { setting.Category });
                    pb.SetCustomAttribute(cab);
                }
            }
        }
    }
}
