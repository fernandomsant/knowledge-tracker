# Note classification service

This service is stateless. It receives note text and the current subject taxonomy and returns GLiClass scores. It does not connect to the Knowledge Tracker database.

```powershell
python -m venv .venv-classifier
.\.venv-classifier\Scripts\python -m pip install -r src\classification-service\requirements.txt
.\.venv-classifier\Scripts\python -m uvicorn app:app --app-dir src\classification-service --host 127.0.0.1 --port 8021
```

Set `GLICLASS_MODEL` to use another compatible model. The default is `knowledgator/gliclass-small-v1.0`.
