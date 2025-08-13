# HybridCache with Redis (L2) + In-Memory (L1) in .NET 9

This project demonstrates how to use **.NET 9 HybridCache** to implement a **two-layer cache** in a normal application:

- **L1** → Local In-Memory Cache (fastest access)
- **L2** → Redis Distributed Cache (shared & persistent)

The goal is:
- Read from memory instantly if available
- Fall back to Redis if memory is empty
- Finally, fall back to the database if both are empty
- Keep Redis as the source of truth for cache data

This setup helps achieve:
- **Ultra-fast local reads** from memory
- **Data consistency** via Redis
- Automatic fallback to database when caches miss

---

## 📜 Architecture
```
Browser (localStorage/IndexedDB)
      │
      ├── Has cached value + ETag? ──► Send GET with If-None-Match: "<etag>"
      │                                │
      │                                ├── 304 Not Modified ──► Use browser store value (no payload)
      │                                │
      │                                └── 200 OK + ETag ─────► Update browser store (value + ETag)
      │
      ▼
Controller → HybridCache.GetOrCreateAsync(key, factory)
      │
      ├── L1 (In-Memory) Cache → ✅ Hit → Return
      │
      ├── L2 (Redis) Cache → ✅ Hit → Store in L1 → Return
      │
      └── Database → Store in L1 + L2 → Return (response includes ETag + Cache-Control)
```

- **L1 Cache (In-Memory)**  
  Local to the application instance. Fastest but not shared between pods.
- **L2 Cache (Redis)**  
  Shared across pods. Slightly slower but consistent.