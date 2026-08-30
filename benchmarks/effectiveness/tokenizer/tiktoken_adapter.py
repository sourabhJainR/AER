#!/usr/bin/env python3
"""Exact tokenizer adapter for the AER benchmark contract.

Reads JSON from stdin:
  {"tokenizer":"o200k_base","texts":{"json":"...","aer":"..."}}
Writes:
  {"tokenizer":"o200k_base","counts":{"json":123,"aer":87}}

Requires: pip install tiktoken
"""
import json
import sys

import tiktoken

request = json.load(sys.stdin)
name = request["tokenizer"]
encoding = tiktoken.get_encoding(name)
counts = {key: len(encoding.encode(text)) for key, text in request["texts"].items()}
json.dump({"tokenizer": name, "counts": counts}, sys.stdout, sort_keys=True)
