package aer

import (
  "bytes"
  "encoding/base64"
  "encoding/binary"
  "fmt"
  "math"
  "strconv"
  "strings"
  "time"
)

type TableData struct { Columns []string; Rows [][]Value }
type Field struct { Name string; Kind Kind; Required bool; Unit string; Min *float64; Max *float64; Meaning string }
type Schema struct { Name string; Fields map[string]Field }
func (s Schema) Validate(v Value) []string { if v.Kind!=Object{return []string{fmt.Sprintf("%s: expected object",s.Name)}}; m:=v.Data.(map[string]Value); var e []string; for n,f:=range s.Fields { x,ok:=m[n]; if !ok {if f.Required {e=append(e,n+": required")}; continue}; if f.Kind!=x.Kind && !(f.Kind==Decimal&&x.Kind==Int) {e=append(e,fmt.Sprintf("%s: expected %d",n,f.Kind));continue}; if f.Min!=nil {if z,ok:=number(x);ok&&z<*f.Min {e=append(e,n+": below minimum")}}; if f.Max!=nil {if z,ok:=number(x);ok&&z>*f.Max {e=append(e,n+": above maximum")}} }; return e }
func number(v Value)(float64,bool){switch x:=v.Data.(type){case int64:return float64(x),true;case float64:return x,true;default:return 0,false}}

func MakeTable(columns []string, rows [][]Value) Value { if len(columns)==0 {panic("empty AER table")}; seen:=map[string]bool{};for _,c:=range columns{if seen[c]{panic("duplicate AER table column")};seen[c]=true};for _,r:=range rows{if len(r)!=len(columns){panic("AER table row width mismatch")}};return Value{Kind:Table,Data:TableData{Columns:append([]string(nil),columns...),Rows:rows}} }

func DumpsFull(v Value) string { var b strings.Builder; b.WriteString("@aer 1\n"); var emit func(string,Value,int); emit=func(k string,x Value,n int){p:=strings.Repeat(" ",n);switch x.Kind{case Object:b.WriteString(p+k+":\n");for a,z:=range x.Data.(map[string]Value){emit(a,z,n+2)};case Array:a:=x.Data.([]Value);if allScalar(a){fmt.Fprintf(&b,"%s%s[%d]:%s\n",p,k,len(a),joinScalars(a))}else{b.WriteString(p+k+":\n");for i,z:=range a{emit(strconv.Itoa(i),z,n+2)}};case Table:t:=x.Data.(TableData);fmt.Fprintf(&b,"%s%s[%d]{%s}:\n",p,k,len(t.Rows),strings.Join(t.Columns,","));for _,r:=range t.Rows{b.WriteString(strings.Repeat(" ",n+2)+joinScalars(r)+"\n")};default:b.WriteString(p+k+":"+scalarFull(x)+"\n")}};if v.Kind==Object{for k,x:=range v.Data.(map[string]Value){emit(k,x,0)}}else{emit("value",v,0)};return b.String() }
func allScalar(a []Value)bool{for _,x:=range a{if x.Kind==Object||x.Kind==Array||x.Kind==Table{return false}};return true}
func joinScalars(a []Value)string{r:=make([]string,len(a));for i,x:=range a{r[i]=scalarFull(x)};return strings.Join(r,",")}
func scalarFull(v Value)string{switch v.Kind{case Null:return "-";case Bool:if v.Data.(bool){return "true"};return "false";case Int:return strconv.FormatInt(v.Data.(int64),10);case Float:return strconv.FormatFloat(v.Data.(float64),'g',-1,64);case Decimal,String: s:=fmt.Sprint(v.Data);if s!=""&&!strings.ContainsAny(s,",:\n")&&s!="-"&&s!="true"&&s!="false"&&!strings.HasPrefix(s,"@"){return s};return `"`+strings.NewReplacer(`\`,`\\`,`"`,`\"`,`\n`,`\\n`).Replace(s)+`"`;case Bytes:return `b64"`+base64.StdEncoding.EncodeToString(v.Data.([]byte))+`"`;case Reference:return "@"+v.Data.(string);case DateTime:return `dt"`+v.Data.(time.Time).UTC().Format(time.RFC3339Nano)+`"`;case Duration:return `dur"`+v.Data.(time.Duration).String()+`"`};panic("non-scalar")}

func EncodeFull(v Value) []byte { b:=bytes.NewBuffer(append([]byte("AERB"),1)); encFull(b,v); return b.Bytes() }
func put64(b *bytes.Buffer,n int64){_ = binary.Write(b,binary.LittleEndian,n)}
func putStr(b *bytes.Buffer,s string){x:=[]byte(s);put64(b,int64(len(x)));b.Write(x)}
func encFull(b *bytes.Buffer,v Value){b.WriteByte(byte(v.Kind));switch v.Kind{case Null:return;case Bool:if v.Data.(bool){b.WriteByte(1)}else{b.WriteByte(0)};case Int:put64(b,v.Data.(int64));case Float:put64(b,int64(math.Float64bits(v.Data.(float64))));case Decimal,String,Reference:putStr(b,fmt.Sprint(v.Data));case Bytes:put64(b,int64(len(v.Data.([]byte))));b.Write(v.Data.([]byte));case DateTime: t:=v.Data.(time.Time).UTC();ticks:=t.UnixNano()/100+621355968000000000;put64(b,ticks);case Duration:put64(b,int64(v.Data.(time.Duration)));case Array:a:=v.Data.([]Value);put64(b,int64(len(a)));for _,x:=range a{encFull(b,x)};case Object:o:=v.Data.(map[string]Value);put64(b,int64(len(o)));for k,x:=range o{putStr(b,k);encFull(b,x)};case Table:t:=v.Data.(TableData);put64(b,int64(len(t.Columns)));for _,c:=range t.Columns{putStr(b,c)};put64(b,int64(len(t.Rows)));for _,r:=range t.Rows{for _,x:=range r{encFull(b,x)}}} }
func DecodeFull(d []byte)(Value,error){if len(d)<5||string(d[:4])!="AERB"||d[4]!=1{return Value{},fmt.Errorf("invalid AERB header")};v,o,e:=decFull(d,5);if e==nil&&o!=len(d){e=fmt.Errorf("trailing AERB payload")};return v,e}
func rd(d []byte,o *int,n int)([]byte,error){if n<0||*o<0||*o>len(d)-n{return nil,fmt.Errorf("truncated AERB payload")};x:=d[*o:*o+n];*o+=n;return x,nil}
func rd64(d []byte,o *int)(int64,error){x,e:=rd(d,o,8);if e!=nil{return 0,e};return int64(binary.LittleEndian.Uint64(x)),nil}
func rdstr(d []byte,o *int)(string,error){n,e:=rd64(d,o);if e!=nil||n<0||n>int64(len(d)-*o){return "",fmt.Errorf("invalid length")};x,e:=rd(d,o,int(n));return string(x),e}
func decFull(d []byte,o int)(Value,int,error){x,e:=rd(d,&o,1);if e!=nil{return Value{},o,e};k:=Kind(x[0]);switch k{case Null:return Value{Null,nil},o,nil;case Bool:x,e=rd(d,&o,1);return Value{Bool,x[0]!=0},o,e;case Int:n,e:=rd64(d,&o);return Value{Int,n},o,e;case Float:n,e:=rd64(d,&o);return Value{Float,math.Float64frombits(uint64(n))},o,e;case Decimal,String,Reference:s,e:=rdstr(d,&o);return Value{k,s},o,e;case Bytes:n,e:=rd64(d,&o);if e!=nil||n<0{return Value{},o,fmt.Errorf("invalid bytes length")};x,e=rd(d,&o,int(n));return Value{Bytes,append([]byte(nil),x...)},o,e;case DateTime:n,e:=rd64(d,&o);return Value{DateTime,time.Unix(0,(n-621355968000000000)*100).UTC()},o,e;case Duration:n,e:=rd64(d,&o);return Value{Duration,time.Duration(n)},o,e;case Array:n,e:=rd64(d,&o);if e!=nil||n<0{return Value{},o,fmt.Errorf("invalid array length")};a:=make([]Value,int(n));for i:=range a{a[i],o,e=decFull(d,o);if e!=nil{return Value{},o,e}};return Value{Array,a},o,nil;case Object:n,e:=rd64(d,&o);if e!=nil||n<0{return Value{},o,e};m:=map[string]Value{};for i:=int64(0);i<n;i++{key,e:=rdstr(d,&o);if e!=nil{return Value{},o,e};z,no,e:=decFull(d,o);o=no;if e!=nil{return Value{},o,e};m[key]=z};return Value{Object,m},o,nil;case Table:c,e:=rd64(d,&o);if e!=nil||c<=0{return Value{},o,fmt.Errorf("invalid column count")};cols:=make([]string,int(c));for i:=range cols{cols[i],e=rdstr(d,&o);if e!=nil{return Value{},o,e}};r,e:=rd64(d,&o);if e!=nil||r<0{return Value{},o,fmt.Errorf("invalid row count")};rows:=make([][]Value,int(r));for i:=range rows{rows[i]=make([]Value,int(c));for j:=range rows[i]{rows[i][j],o,e=decFull(d,o);if e!=nil{return Value{},o,e}}};return MakeTable(cols,rows),o,nil};return Value{},o,fmt.Errorf("unsupported AER kind %d",k)}

func ApplyPatch(root Value, ops []PatchOperation) Value {cur:=root;for _,op:=range ops{cur=patchOne(cur,op)};return cur}
type PatchOp string
const(Add PatchOp="add"; Replace PatchOp="replace"; Remove PatchOp="remove")
type PatchOperation struct{Op PatchOp; Path string; Value *Value}
func patchOne(root Value,op PatchOperation)Value{p:=[]string{};for _,s:=range strings.Split(op.Path,"/"){if s!=""{p=append(p,s)}};if len(p)==0{panic("empty patch path")};return mutatePatch(root,p,0,op)}
func mutatePatch(n Value,p []string,i int,op PatchOperation)Value{if i==len(p)-1{return patchLeaf(n,p[i],op)};k:=p[i];switch n.Kind{case Object:m:=cloneObj(n.Data.(map[string]Value));child,ok:=m[k];if !ok{panic("patch path does not exist")};m[k]=mutatePatch(child,p,i+1,op);return Value{Object,m};case Array:a:=append([]Value(nil),n.Data.([]Value)...);idx,_:=strconv.Atoi(k);a[idx]=mutatePatch(a[idx],p,i+1,op);return Value{Array,a};default:panic("patch target is not traversable")}}
func patchLeaf(n Value,k string,op PatchOperation)Value{switch n.Kind{case Object:m:=cloneObj(n.Data.(map[string]Value));if op.Op==Remove{if _,ok:=m[k];!ok{panic("patch field does not exist")};delete(m,k)}else{if op.Value==nil{panic("patch value required")};m[k]=*op.Value};return Value{Object,m};case Array:a:=append([]Value(nil),n.Data.([]Value)...);idx,_:=strconv.Atoi(k);if op.Op==Remove{a=append(a[:idx],a[idx+1:]...)}else{if op.Value==nil{panic("patch value required")};a[idx]=*op.Value};return Value{Array,a};default:panic("patch target is not mutable")}}
func cloneObj(m map[string]Value)map[string]Value{n:=map[string]Value{};for k,v:=range m{n[k]=v};return n}

const frameHeader=9
func EncodeFrame(v Value)[]byte{p:=EncodeFull(v);o:=make([]byte,frameHeader+len(p));copy(o,[]byte("AERF"));o[4]=1;binary.LittleEndian.PutUint32(o[5:9],uint32(len(p)));copy(o[9:],p);return o}
func DecodeFrames(d []byte,max int) ([]Value,error){var out []Value;for o:=0;o<len(d);{if len(d)-o<frameHeader{return nil,fmt.Errorf("AER008 truncated frame header")};if string(d[o:o+4])!="AERF"||d[o+4]!=1{return nil,fmt.Errorf("AER009 invalid frame")};n:=int(binary.LittleEndian.Uint32(d[o+5:o+9]));if n>max||n<0{return nil,fmt.Errorf("AER006 frame too large")};if len(d)-o-frameHeader<n{return nil,fmt.Errorf("AER008 truncated frame payload")};v,e:=DecodeFull(d[o+9:o+9+n]);if e!=nil{return nil,e};out=append(out,v);o+=frameHeader+n};return out,nil}
