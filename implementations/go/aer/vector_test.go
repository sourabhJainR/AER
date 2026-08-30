package aer

import("encoding/hex";"reflect";"testing")

func TestFrozenBinaryTableVector(t *testing.T){
  hexv:="41455242010b02000000000000000200000000000000696404000000000000006e616d650100000000000000020100000000000000050400000000000000416d6974"
  raw,_:=hex.DecodeString(hexv); got,e:=DecodeFull(raw);if e!=nil{t.Fatal(e)}
  expected:=MakeTable([]string{"id","name"},[][]Value{{{Int,int64(1)},{String,"Amit"}}})
  if !reflect.DeepEqual(got,expected){t.Fatalf("vector mismatch: %#v",got)}
}
