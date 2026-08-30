package aer

import (
	"encoding/json"
	"fmt"
	"reflect"
	"strings"
)

func Scalar(v any) string {
	if v == nil { return "-" }
	if b, ok := v.(bool); ok { if b { return "true" }; return "false" }
	switch x := v.(type) {
	case string:
		if strings.ContainsAny(x, ",:\n\"{}[]") { b, _ := json.Marshal(x); return string(b) }
		return x
	case int, int8, int16, int32, int64, uint, uint8, uint16, uint32, uint64, float32, float64:
		return fmt.Sprint(x)
	default:
		b, _ := json.Marshal(v); return string(b)
	}
}

func Encode(value map[string]any, indent string) string {
	var out []string
	for key, item := range value {
		rv := reflect.ValueOf(item)
		if rv.IsValid() && rv.Kind() == reflect.Slice && rv.Len() > 0 && rv.Index(0).Kind() == reflect.Map {
			// Reference emitter only; full schema-aware table normalization belongs in the Go SDK.
			out = append(out, fmt.Sprintf("%s%s:%s", indent, key, Scalar(item)))
		} else if m, ok := item.(map[string]any); ok {
			out = append(out, indent+key+":")
			out = append(out, strings.Split(Encode(m, indent+"  "), "\n")...)
		} else {
			out = append(out, fmt.Sprintf("%s%s:%s", indent, key, Scalar(item)))
		}
	}
	return strings.Join(out, "\n")
}
