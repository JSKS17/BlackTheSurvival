using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

// Read installed Unity's managed IL without invoking any Unity native GUI API.
// This verifies the host-double contract against the exact editor being built.
public static class CombatCardTouchUnityBoundaryTests
{
    static readonly Dictionary<short,OpCode> opcodes=new Dictionary<short,OpCode>();
    public static int Main()
    {
        foreach(var f in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static))
            if(f.FieldType==typeof(OpCode)){var opcode=(OpCode)f.GetValue(null);opcodes[opcode.Value]=opcode;}
        var method=typeof(GUI).GetMethod("DoControl",BindingFlags.Static|BindingFlags.NonPublic);
        if(method==null)throw new Exception("Installed Unity GUI.DoControl is unavailable");
        var il=method.GetMethodBody().GetILAsByteArray();
        var calls=new List<string>();var switchTypes=new List<string>();
        for(int offset=0;offset<il.Length;)
        {
            short code=il[offset++];if(code==0xfe)code=(short)(0xfe00|il[offset++]);
            var opcode=opcodes[code];
            if(opcode.OperandType==OperandType.InlineSwitch)
            {
                int count=BitConverter.ToInt32(il,offset);offset+=4;
                var targets=new int[count];for(int i=0;i<count;i++)targets[i]=BitConverter.ToInt32(il,offset+i*4);
                // The shared fallback target belongs to ignored key/mouse events.
                // Record every enum routed somewhere different from MouseMove.
                int fallback=count>2?targets[2]:int.MinValue;
                for(int i=0;i<count;i++)if(targets[i]!=fallback)switchTypes.Add(((EventType)i).ToString());
                offset+=count*4;continue;
            }
            if(opcode.OperandType==OperandType.InlineMethod)
            {var callee=method.Module.ResolveMethod(BitConverter.ToInt32(il,offset));calls.Add(callee.Name);}
            switch(opcode.OperandType)
            {
                case OperandType.InlineNone:break;
                case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:offset++;break;
                case OperandType.InlineVar:offset+=2;break;
                case OperandType.InlineI8:case OperandType.InlineR:offset+=8;break;
                default:offset+=4;break;
            }
        }
        foreach(string name in new[]{"MouseDown","MouseUp","MouseDrag"})
            if(!switchTypes.Exists(x=>string.Equals(x,name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Installed DoControl lacks expected "+name+" branch");
        foreach(string name in new[]{"TouchDown","TouchMove","TouchUp"})
            if(switchTypes.Exists(x=>string.Equals(x,name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Raw touch now handled by button; update the host double");
        foreach(string name in new[]{"GrabMouseControl","HasMouseControl","ReleaseMouseControl"})
            if(!calls.Contains(name))throw new Exception("Installed DoControl capture contract differs: "+name);
        Console.WriteLine("UNITY_GUI_BOUNDARY_RESULT {\"result\":\"PASS\",\"assemblyVersion\":\""+typeof(GUI).Assembly.GetName().Version+"\",\"assembly\":\"UnityEngine.IMGUIModule\",\"buttonEventBranches\":[\""+string.Join("\",\"",switchTypes.ToArray())+"\"],\"rawTouchButtonCapture\":false,\"captureContract\":\"GrabMouseControl/HasMouseControl/ReleaseMouseControl\"}");
        return 0;
    }
}
