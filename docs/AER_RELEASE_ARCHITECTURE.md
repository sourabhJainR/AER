# AER release architecture

```text
                 AER canonical model
                         |
          +--------------+--------------+
          |              |              |
        AER-H          AER-AI         AER-B
       human             AI           binary
          |              |              |
          +--------------+--------------+
                         |
               frozen conformance
                         |
        +--------+-------+-------+-------+
        |        |       |       |       |
       .NET   Python   TS      Go    future Rust
```

The canonical model and frozen AER-B vectors are the compatibility anchor. Language implementations remain independently maintained so a single reference implementation is not a hidden dependency.
