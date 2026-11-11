// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain it at https://mozilla.org/MPL/2.0/.
// C# adaptation of USB HID brightness control in sfjohnson/studio-brightness; see NOTICE.md.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class StudioDisplayBrightness {
 const int Success=0x110000;
 [StructLayout(LayoutKind.Sequential)] struct DeviceInterface { public uint Size; public Guid Guid; public uint Flags; public IntPtr Reserved; }
 public class ValueInfo { public ushort Page,Usage,Collection,Bits,Count; public byte ReportId; public int Min,Max; public bool Range; }
 public class Result { public string Device; public ushort Page,Usage,InputLength,FeatureLength; public ValueInfo[] Inputs,Features; public uint Before,After; public int Requested=-1; public bool Written; }
 [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid guid);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr SetupDiGetClassDevsW(ref Guid guid,IntPtr enumerator,IntPtr parent,uint flags);
 [DllImport("setupapi.dll",SetLastError=true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set,IntPtr info,ref Guid guid,uint index,ref DeviceInterface data);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr set,ref DeviceInterface data,IntPtr detail,uint size,out uint required,IntPtr info);
 [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFileW(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
 [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetPreparsedData(SafeFileHandle device,out IntPtr data);
 [DllImport("hid.dll")] [return:MarshalAs(UnmanagedType.U1)] static extern bool HidD_FreePreparsedData(IntPtr data);
 [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr data,IntPtr caps);
 [DllImport("hid.dll")] static extern int HidP_GetValueCaps(int type,IntPtr caps,ref ushort count,IntPtr data);
 [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetInputReport(SafeFileHandle device,[In,Out] byte[] report,uint size);
 [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetFeature(SafeFileHandle device,[In,Out] byte[] report,uint size);
 [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.U1)] static extern bool HidD_SetFeature(SafeFileHandle device,byte[] report,uint size);
 [DllImport("hid.dll")] static extern int HidP_GetUsageValue(int type,ushort page,ushort collection,ushort usage,out uint value,IntPtr data,byte[] report,uint size);
 [DllImport("hid.dll")] static extern int HidP_SetUsageValue(int type,ushort page,ushort collection,ushort usage,uint value,IntPtr data,[In,Out] byte[] report,uint size);
 static ushort U16(IntPtr p,int o) {return unchecked((ushort)Marshal.ReadInt16(p,o));}
 static void Status(int s,string a) {if(s!=Success) throw new Exception(a+": HID status 0x"+s.ToString("X8"));}
 static Exception Error(string a) {return new Win32Exception(Marshal.GetLastWin32Error(),a);}
 static string Find() {
  Guid guid; HidD_GetHidGuid(out guid);
  IntPtr set=SetupDiGetClassDevsW(ref guid,IntPtr.Zero,IntPtr.Zero,0x12);
  if(set==new IntPtr(-1)) throw Error("Enumerating HID devices");
  var matches=new List<string>();
  try {
   for(uint i=0;;i++) {
    var d=new DeviceInterface {Size=(uint)Marshal.SizeOf(typeof(DeviceInterface))};
    if(!SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref d)) {if(Marshal.GetLastWin32Error()==259) break; throw Error("Reading HID interface");}
    uint required; SetupDiGetDeviceInterfaceDetailW(set,ref d,IntPtr.Zero,0,out required,IntPtr.Zero);
    if(required<8) throw new Exception("Invalid HID path buffer size");
    IntPtr p=Marshal.AllocHGlobal((int)required);
    try {
     Marshal.WriteInt32(p,IntPtr.Size==8?8:6);
     if(!SetupDiGetDeviceInterfaceDetailW(set,ref d,p,required,out required,IntPtr.Zero)) throw Error("Reading HID path");
     string path=Marshal.PtrToStringUni(IntPtr.Add(p,4)), low=path.ToLowerInvariant();
     if(low.Contains("vid_05ac&pid_1114&mi_07#")&&!low.Contains("&col")) matches.Add(path);
    } finally {Marshal.FreeHGlobal(p);}
   }
  } finally {SetupDiDestroyDeviceInfoList(set);}
  if(matches.Count!=1) throw new Exception("Expected one Studio Display brightness interface; found "+matches.Count);
  return matches[0];
 }
 static ValueInfo[] Values(IntPtr data,int type,ushort count) {
  if(count==0) return new ValueInfo[0];
  IntPtr buffer=Marshal.AllocHGlobal(72*count);
  try {
   Status(HidP_GetValueCaps(type,buffer,ref count,data),"Reading HID value caps");
   var result=new ValueInfo[count];
   for(int i=0;i<count;i++) {
    IntPtr p=IntPtr.Add(buffer,72*i);
    result[i]=new ValueInfo {Page=U16(p,0),ReportId=Marshal.ReadByte(p,2),Collection=U16(p,6),Range=Marshal.ReadByte(p,12)!=0,Bits=U16(p,18),Count=U16(p,20),Min=Marshal.ReadInt32(p,40),Max=Marshal.ReadInt32(p,44),Usage=U16(p,56)};
   }
   return result;
  } finally {Marshal.FreeHGlobal(buffer);}
 }
 static uint Read(SafeFileHandle device,IntPtr data,ushort length,ValueInfo info) {
  byte[] report=new byte[Math.Max((int)length,100)]; report[0]=info.ReportId;
  if(!HidD_GetInputReport(device,report,(uint)report.Length)) throw Error("Reading current brightness");
  uint value; Status(HidP_GetUsageValue(0,info.Page,info.Collection,info.Usage,out value,data,report,length),"Decoding current brightness"); return value;
 }
 public static Result Run(int target=-1) {
  string path=Find();
  using(var device=CreateFileW(path,0xC0000000,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
   if(device.IsInvalid) throw Error("Opening Studio Display brightness interface");
   IntPtr data; if(!HidD_GetPreparsedData(device,out data)) throw Error("Reading HID descriptor");
   try {
    IntPtr caps=Marshal.AllocHGlobal(64); var r=new Result {Device=path};
    try {
     Status(HidP_GetCaps(data,caps),"Reading HID caps");
     r.Usage=U16(caps,0);r.Page=U16(caps,2);r.InputLength=U16(caps,4);r.FeatureLength=U16(caps,8);
     r.Inputs=Values(data,0,U16(caps,48));r.Features=Values(data,2,U16(caps,60));
    } finally {Marshal.FreeHGlobal(caps);}
    if(r.Inputs.Length<1||r.Features.Length<1) throw new Exception("Brightness HID values missing");
    var input=r.Inputs[0];var feature=r.Features[0];
    if(input.Range||feature.Range||input.Count!=1||feature.Count!=1) throw new Exception("Unexpected brightness HID descriptor");
    r.Before=Read(device,data,r.InputLength,input);r.After=r.Before;
    if(target>=0) {
     if(target<400||target>60000||target<feature.Min||target>feature.Max) throw new Exception("Brightness target outside verified safe range");
     byte[] report=new byte[Math.Max((int)r.FeatureLength,100)];report[0]=feature.ReportId;
     if(!HidD_GetFeature(device,report,(uint)report.Length)) throw Error("Reading existing brightness feature");
     Status(HidP_SetUsageValue(2,feature.Page,feature.Collection,feature.Usage,(uint)target,data,report,r.FeatureLength),"Encoding brightness");
     if(!HidD_SetFeature(device,report,r.FeatureLength)) throw Error("Setting Studio Display brightness");
     r.Requested=target;r.Written=true;System.Threading.Thread.Sleep(300);r.After=Read(device,data,r.InputLength,input);
    }
    return r;
   } finally {HidD_FreePreparsedData(data);}
  }
 }
}
