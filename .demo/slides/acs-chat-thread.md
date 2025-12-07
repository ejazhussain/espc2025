---
theme: espc25
layout: default
---

## Step2: Thread Setup

</br>
</br>

```mermaid

graph LR
    B[Backend] -->|Create| T[Thread]
    T -->|Connect| A[Client A]
    T -->|Connect| C[Client B]
```