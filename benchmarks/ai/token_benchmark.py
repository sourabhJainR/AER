import argparse, json, importlib.util, platform, sys
from pathlib import Path
from aer import dumps

def tokenizer_count(text, name):
    if name == 'tiktoken':
        import tiktoken
        enc=tiktoken.get_encoding('cl100k_base')
        return len(enc.encode(text))
    if name == 'o200k':
        import tiktoken
        enc=tiktoken.get_encoding('o200k_base')
        return len(enc.encode(text))
    if name == 'transformers':
        from transformers import AutoTokenizer
        tok=AutoTokenizer.from_pretrained('gpt2')
        return len(tok.encode(text, add_special_tokens=False))
    raise ValueError(name)

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--input',default='benchmarks/ai/sample.json')
    ap.add_argument('--tokenizer',action='append',default=['tiktoken'])
    ap.add_argument('--out',default='benchmarks/ai/results.json')
    a=ap.parse_args(); raw=Path(a.input).read_text(); obj=json.loads(raw)
    json_text=json.dumps(obj,separators=(',',':'),ensure_ascii=False); aer_text=dumps(obj,header=False)
    results={'python':sys.version,'platform':platform.platform(),'input_bytes':len(raw.encode()),'formats':{'json':json_text,'aer':aer_text},'tokenizers':{}}
    for name in a.tokenizer:
        try: results['tokenizers'][name]={'json_tokens':tokenizer_count(json_text,name),'aer_tokens':tokenizer_count(aer_text,name)}
        except Exception as ex: results['tokenizers'][name]={'error':str(ex)}
    Path(a.out).write_text(json.dumps(results,indent=2));print(json.dumps(results['tokenizers'],indent=2))
if __name__=='__main__': main()
