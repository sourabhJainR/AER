package aer

import("testing";"math/rand";"reflect")

func TestFullParity(t *testing.T){
  sample:=Value{Object,map[string]Value{"user":Value{Object,map[string]Value{"id":Value{Int,int64(1)},"name":Value{String,"Amit"}}},"employees":MakeTable([]string{"id","name"},[][]Value{{{Int,int64(1)},{String,"Amit"}},{{Int,int64(2)},{String,"Priya"}}})}}
  got,e:=DecodeFull(EncodeFull(sample));if e!=nil||!reflect.DeepEqual(got,sample){t.Fatalf("binary parity failed: %v %#v",e,got)}
  f:=EncodeFrame(sample);vals,e:=DecodeFrames(append(f,f...),16*1024*1024);if e!=nil||len(vals)!=2{t.Fatalf("stream parity failed: %v",e)}
  patched:=ApplyPatch(sample,[]PatchOperation{{Op:Replace,Path:"/user/name",Value:&Value{String,"Priya"}}});if patched.Data.(map[string]Value)["user"].Data.(map[string]Value)["name"].Data!="Priya"{t.Fatal("patch failed")}
  s:=Schema{Name:"User",Fields:map[string]Field{"id":{Name:"id",Kind:Int,Required:true},"name":{Name:"name",Kind:String,Required:true}}};if len(s.Validate(sample.Data.(map[string]Value)["user"]))!=0{t.Fatal("schema validation failed")}
}

func TestFullPropertyRoundTrip(t *testing.T){r:=rand.New(rand.NewSource(20260830));for i:=0;i<250;i++{x:=Value{Object,map[string]Value{"id":{Int,int64(r.Intn(200)-100)},"ok":{Bool,r.Intn(2)==1},"name":{String,[]string{"a","b","a,b"}[r.Intn(3)]}}};got,e:=DecodeFull(EncodeFull(x));if e!=nil||!reflect.DeepEqual(got,x){t.Fatalf("case %d failed: %v",i,e)}}}

func FuzzAERBinary(f *testing.F){f.Add([]byte("AERB\x01\x00"));f.Fuzz(func(t *testing.T,data []byte){if len(data)>1024*1024{return};_,_=DecodeFull(data)})}
