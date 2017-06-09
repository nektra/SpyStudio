PARSE_METHOD(QueryInterface, (__in IClassFactory *This, __in REFIID riid, __deref_out LPVOID *ppvObject), (This, riid, ppvObject), 2)

PARSE_METHOD(CreateInstance, (__in IClassFactory * This, __in IUnknown *pUnkOuter, __in REFIID riid, __deref_out LPVOID *ppvObject), (This, pUnkOuter, riid, ppvObject), 3)
PARSE_METHOD(LockServer, (__in IClassFactory * This, __in BOOL fLock), (This, fLock), 1)
