//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: grSim_Replacement.proto
namespace SSLRig.Core.Data.Packet
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"grSim_RobotReplacement")]
  public partial class grSim_RobotReplacement : global::ProtoBuf.IExtensible
  {
    public grSim_RobotReplacement() {}
    
    private double _x;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"x", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double x
    {
      get { return _x; }
      set { _x = value; }
    }
    private double _y;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"y", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double y
    {
      get { return _y; }
      set { _y = value; }
    }
    private double _dir;
    [global::ProtoBuf.ProtoMember(3, IsRequired = true, Name=@"dir", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double dir
    {
      get { return _dir; }
      set { _dir = value; }
    }
    private uint _id;
    [global::ProtoBuf.ProtoMember(4, IsRequired = true, Name=@"id", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint id
    {
      get { return _id; }
      set { _id = value; }
    }
    private bool _yellowteam;
    [global::ProtoBuf.ProtoMember(5, IsRequired = true, Name=@"yellowteam", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public bool yellowteam
    {
      get { return _yellowteam; }
      set { _yellowteam = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"grSim_BallReplacement")]
  public partial class grSim_BallReplacement : global::ProtoBuf.IExtensible
  {
    public grSim_BallReplacement() {}
    
    private double _x;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"x", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double x
    {
      get { return _x; }
      set { _x = value; }
    }
    private double _y;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"y", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double y
    {
      get { return _y; }
      set { _y = value; }
    }
    private double _vx;
    [global::ProtoBuf.ProtoMember(3, IsRequired = true, Name=@"vx", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double vx
    {
      get { return _vx; }
      set { _vx = value; }
    }
    private double _vy;
    [global::ProtoBuf.ProtoMember(4, IsRequired = true, Name=@"vy", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public double vy
    {
      get { return _vy; }
      set { _vy = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"grSim_Replacement")]
  public partial class grSim_Replacement : global::ProtoBuf.IExtensible
  {
    public grSim_Replacement() {}
    

    private grSim_BallReplacement _ball = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ball", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public grSim_BallReplacement ball
    {
      get { return _ball; }
      set { _ball = value; }
    }
    private readonly global::System.Collections.Generic.List<grSim_RobotReplacement> _robots = new global::System.Collections.Generic.List<grSim_RobotReplacement>();
    [global::ProtoBuf.ProtoMember(2, Name=@"robots", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<grSim_RobotReplacement> robots
    {
      get { return _robots; }
    }
  
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
