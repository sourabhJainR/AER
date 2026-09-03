# HTTP content negotiation

AER can be introduced at an HTTP representation boundary without changing domain models.

Recommended negotiation:

```http
Accept: application/aer
Accept: application/aer; profile="ai"
Accept: application/json
```

The server should select the most appropriate representation supported by both parties and retain JSON as the fallback. AER should not require a custom HTTP transport or custom client library for basic negotiation.
