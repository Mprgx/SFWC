import uuid
import requests


def _seed_payment(user_session, amount=5.0):
    """Create a payment and return (tx_id, secret_hash)."""
    headers = {"Authorization": user_session["session_token"]}
    tx = "tx-" + uuid.uuid4().hex[:12]
    r = requests.post(
        user_session["url"] + "payments",
        json={"transaction": tx, "amount": amount},
        headers=headers,
    )
    assert r.status_code == 201, f"{r.status_code} {r.text}"
    p = r.json()["payment"]
    return tx, p["hash"]


def test_put_payment_requires_auth(user_session):
    pid = "tx-" + uuid.uuid4().hex[:12]
    body = {"t_data": {"note": "x"}, "validation": "abc"}
    resp = requests.put(user_session["url"] + f"payments/{pid}", json=body)
    assert resp.status_code == 401


def test_put_payment_missing_fields_returns_400(user_session):
    pid, secret = _seed_payment(user_session)
    headers = {"Authorization": user_session["session_token"]}

    r1 = requests.put(
        user_session["url"] + f"payments/{pid}",
        json={"validation": secret},
        headers=headers,
    )
    assert r1.status_code == 400, f"Expected 400, got {r1.status_code}: {r1.text}"

    r2 = requests.put(
        user_session["url"] + f"payments/{pid}",
        json={"t_data": {"ok": True}},
        headers=headers,
    )
    assert r2.status_code == 400, f"Expected 400, got {r2.status_code}: {r2.text}"


def test_put_payment_not_found_returns_404(user_session):
    headers = {"Authorization": user_session["session_token"]}
    fake_pid = "tx-" + uuid.uuid4().hex[:12]
    body = {"t_data": {"note": "nope"}, "validation": "whatever"}
    resp = requests.put(user_session["url"] + f"payments/{fake_pid}", json=body, headers=headers)
    assert resp.status_code == 404, f"Expected 404, got {resp.status_code}: {resp.text}"


def test_put_payment_wrong_validation_returns_401(user_session):
    pid, _secret = _seed_payment(user_session)
    headers = {"Authorization": user_session["session_token"]}
    body = {"t_data": {"ok": False}, "validation": "WRONG"}
    r = requests.put(user_session["url"] + f"payments/{pid}", json=body, headers=headers)
    assert r.status_code == 401


def test_put_payment_happy_path_marks_completed_and_echoes_tdata(user_session):
    pid, secret = _seed_payment(user_session, amount=12.34)
    headers = {"Authorization": user_session["session_token"]}
    t_data = {"psp": "mock", "id": "abc123"}

    upd = requests.put(
        user_session["url"] + f"payments/{pid}",
        json={"t_data": t_data, "validation": secret},
        headers=headers,
    )
    assert upd.status_code == 200, f"{upd.status_code} {upd.text}"
    assert upd.headers.get("Content-Type", "").startswith("application/json")

    body = upd.json()
    assert body.get("status") == "Success"
    payment = body.get("payment")
    assert isinstance(payment, dict)
    assert payment.get("transaction") == pid
    assert payment.get("t_data") == t_data
    assert isinstance(payment.get("completed"), str) and payment["completed"]
