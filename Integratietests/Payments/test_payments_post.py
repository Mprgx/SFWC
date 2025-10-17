import uuid
import requests

def test_post_payments_requires_auth(user_session):
    url = user_session['url'] + 'payments'
    payload = {"transaction": uuid.uuid4().hex[:12], "amount": 15.5}
    r = requests.post(url, json=payload)
    assert r.status_code == 401

def test_post_payments_creates_payment(user_session):
    username = user_session["username"]
    url = user_session['url']
    headers = {"Authorization": user_session["session_token"]}
    payload = {"transaction": uuid.uuid4().hex[:12], "amount": 7.5}

    r = requests.post(url + 'payments', json=payload, headers=headers)
    assert r.status_code == 201, f"{r.status_code} {r.text}"
    assert r.headers.get("Content-Type", "").startswith("application/json")

    data = r.json()
    assert data.get("status") == "Success"
    assert "payment" in data and isinstance(data["payment"], dict)

    payment = data["payment"]
    assert payment.get("transaction") == payload["transaction"]
    assert payment.get("amount") == payload["amount"]
    assert payment.get("initiator") == username
    assert payment.get("completed") is False
    assert isinstance(payment.get("hash"), str) and payment["hash"]

def test_post_payments_missing_transaction_returns_400(user_session):
    url = user_session['url']
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, + 'payments', json={"amount": 1.0}, headers=headers)
    assert r.status_code == 400, f"Expected 400 for missing transaction. {r.status_code}: {r.text}"

def test_post_payments_missing_amount_returns_400(user_session):
    url = user_session['url']
    headers = {"Authorization": user_session["session_token"]}
    payload = {"transaction": uuid.uuid4().hex[:12]}

    r = requests.post(url + 'payments', json=payload, headers=headers)
    assert r.status_code == 400, f"Expected 400 for missing amount. {r.status_code}: {r.text}"

def test_post_payments_amount_must_be_number_returns_400(user_session):
    url = user_session['url']
    headers = {"Authorization": user_session["session_token"]}
    payload =  {"transaction": uuid.uuid4().hex[:12], "amount": "invalidamount"}

    r = requests.post(url + 'payments', json=payload, headers=headers)
    assert r.status_code == 400, f"Expected 400 for non numeric amount. {r.status_code}: {r.text}"

def test_post_payments_allows_decimal_amount(user_session):
    url = user_session['url']
    headers = {"Authorization": user_session["session_token"]}
    payload = {"transaction": uuid.uuid4().hex[:12], "amount": 9.99}

    r = requests.post(url + 'payments', json=payload, headers=headers)
    assert r.status_code == 201, f"{r.status_code}: {r.text}"

    data = r.json()
    assert isinstance(data.get("payment"), dict)
    assert data["payment"].get("amount") == 9.99