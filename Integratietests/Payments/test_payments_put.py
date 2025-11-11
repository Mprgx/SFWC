import uuid
import requests


def _new_pid():
    return uuid.uuid4().hex[:12]

def _seed_payment(user_session, amount=5.0):
    headers = {"Authorization": user_session["session_token"]}
    pid = _new_pid()
    resp = requests.post(
        user_session["url"] + "payments",
        json={"transaction": pid, "amount": amount},
        headers=headers,
    )
    assert resp.status_code == 201, f"{resp.status_code} {resp.text}"
    assert resp.headers.get("Content-Type", "").startswith("application/json")
    payment = resp.json()["payment"]
    return pid, payment["hash"]

def test_put_payments_requires_auth(user_session):
    pid = _new_pid()
    body = {"t_data": {"note": "x"}, "validation": "abc"}

    resp = requests.put(user_session["url"] + f"payments/{pid}", json=body)

    assert resp.status_code == 401
    assert resp.headers.get("Content-Type", "").startswith("application/json")

def test_put_payments_missing_fields_returns_400(user_session):
    pid, secret = _seed_payment(user_session)
    headers = {"Authorization": user_session["session_token"]}

    r1 = requests.put(
        user_session["url"] + f"payments/{pid}",
        json={"validation": secret},
        headers=headers,
    )
    assert r1.status_code == 400, f"Expected 400, got {r1.status_code}: {r1.text}"
    assert r1.headers.get("Content-Type", "").startswith("application/json")

    r2 = requests.put(
        user_session["url"] + f"payments/{pid}",
        json={"t_data": {"ok": True}},
        headers=headers,
    )
    assert r2.status_code == 400, f"Expected 400, got {r2.status_code}: {r2.text}"
    assert r2.headers.get("Content-Type", "").startswith("application/json")

def test_put_payments_not_found_returns_404(user_session):
    headers = {"Authorization": user_session["session_token"]}
    fake_pid = _new_pid()
    body = {"t_data": {"note": "nope"}, "validation": "whatever"}

    resp = requests.put(user_session["url"] + f"payments/{fake_pid}", json=body, headers=headers)

    assert resp.status_code == 404, f"Expected 404, got {resp.status_code}: {resp.text}"
    assert resp.headers.get("Content-Type", "").startswith("application/json")

def test_put_payments_wrong_validation_returns_401(user_session):
    pid, _secret = _seed_payment(user_session)
    headers = {"Authorization": user_session["session_token"]}
    body = {"t_data": {"ok": False}, "validation": "WRONG"}

    resp = requests.put(user_session["url"] + f"payments/{pid}", json=body, headers=headers)

    assert resp.status_code == 401
    assert resp.headers.get("Content-Type", "").startswith("application/json")

def test_put_payments_marks_completed_and_echoes_tdata(user_session):
    pid, secret = _seed_payment(user_session, amount=12.34)
    headers = {"Authorization": user_session["session_token"]}
    t_data = {"psp": "mock", "id": "abc123"}

    resp = requests.put(
        user_session["url"] + f"payments/{pid}",
        json={"t_data": t_data, "validation": secret},
        headers=headers,
    )

    assert resp.status_code == 200, f"{resp.status_code} {resp.text}"
    assert resp.headers.get("Content-Type", "").startswith("application/json")

    body = resp.json()
    assert body.get("status") == "Success"
    payment = body.get("payment")
    assert isinstance(payment, dict)
    assert payment.get("transaction") == pid
    assert payment.get("t_data") == t_data
    assert isinstance(payment.get("completed"), str) and payment["completed"]
