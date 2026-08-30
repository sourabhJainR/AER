package aer

import("reflect";"testing")

func TestFullTextTableRoundTrip(t *testing.T){x:=Value{Object,map[string]Value{"employees":MakeTable([]string{"id","name"},[][]Value{{{Int,1},{String,"Amit"}},{ {Int,2},{String,"Priya"}}})}};text:=DumpsFull(x);got,e:=LoadsFull(text);if e!=nil{t.Fatal(e)};if !reflect.DeepEqual(got,x){t.Fatalf("text roundtrip mismatch: %#v vs %#v",got,x)}}
