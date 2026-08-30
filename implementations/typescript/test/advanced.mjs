import assert from 'node:assert/strict';
import { AerKind, v, object, table, encodeBinary, decodeBinary, applyPatch, encodeFrame, decodeFrames, validate, dumps, loadsAdvanced } from '../dist/advanced.js';

const sample=object({user:object({id:v(AerKind.Int,1n),name:v(AerKind.String,'Amit')}), employees:table(['id','name'],[[v(AerKind.Int,1n),v(AerKind.String,'Amit')],[v(AerKind.Int,2n),v(AerKind.String,'Priya')]])});
assert.deepEqual(decodeBinary(encodeBinary(sample)),sample);
const parsed=loadsAdvanced(dumps(sample));
assert.equal(parsed.kind,AerKind.Object);
assert.equal(parsed.data.employees.kind,AerKind.Table);
const schema={name:'User',fields:{id:{name:'id',kind:AerKind.Int,required:true,min:0},name:{name:'name',kind:AerKind.String,required:true}}};
assert.deepEqual(validate(schema,sample.data.user),[]);
const patched=applyPatch(sample,[{op:'replace',path:'/user/name',value:v(AerKind.String,'Priya')}]);
assert.equal(patched.data.user.data.name.data,'Priya');
assert.equal(decodeFrames(new Uint8Array([...encodeFrame(sample),...encodeFrame(patched)])).length,2);
assert.match(dumps(sample),'employees[2]{id,name}:');

let seed=20260830; const rnd=()=>{seed=(1664525*seed+1013904223)>>>0;return seed/2**32};
for(let i=0;i<250;i++){const x=object({id:v(AerKind.Int,BigInt(Math.floor(rnd()*100)-50)),ok:v(AerKind.Bool,rnd()>0.5),name:v(AerKind.String,['a','b','a,b'][Math.floor(rnd()*3)])});assert.deepEqual(decodeBinary(encodeBinary(x)),x);assert.deepEqual(loadsAdvanced(dumps(x)),x);}
console.log('advanced parity/property tests passed');
