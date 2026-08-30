package aer

import("encoding/hex";"testing")

func TestFrozenBinaryVectors(t *testing.T){vectors:=[]struct{id,hex string}{{"null","414552420100"},{"object-basic","41455242010a030000000000000004000000000000006e616d65050700000000000000536f75726162680300000000000000616765022d0000000000000006000000000000006163746976650101"},{"mixed-array","41455242010903000000000000000201000000000000000501000000000000007800"}};for _,v:=range vectors{b,e:=hex.DecodeString(v.hex);if e!=nil{t.Fatal(e)};if _,e=Decode(b);e!=nil{t.Fatalf("%s: %v",v.id,e)}}}
