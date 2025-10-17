import uuid
import requests

def test_get_payments_requires_auth(user_session):
    url = user_session['url'] + 'payments'
    r = requests.get(url)
    assert r.status_code == 401

def test_get_payments_returns_user_payments(user_session):
    url = user_session['url'] + 'payments'
    headers = {"Authorization":user_session["session_token"]}
    payload = {"transaction": uuid.uuid4().hex[:12], "amount": 75.25}

    seed_response = requests.post(url, json=payload, headers=headers)
    assert seed_response.status_code == 201, f"{seed_response.status_code}: {seed_response.text}"

    get_response = requests.get(url, headers=headers)
    assert get_response.status_code == 200
    assert get_response.headers.get("Content-Type", "").startswith("application/json")

    payments = get_response.json()
    assert isinstance(payments, list), "GET /payments should return a JSON array"

    matches = [p for p in payments if p.get("transaction") == payload["transaction"]]
    assert matches, f"Expected seeded transaction {payload['transaction']} in GET /payments result"

    payment = matches[0]
    assert "transaction" in payment
    assert "amount" in payment
    assert "initiator" in payment

def test_get_payments_user_requires_auth(user_session):
    response = requests.get(user_session['url'] + f"payments/{user_session['username']}")
    assert response.status_code == 401

def test_get_payments_user_requires_admin(user_session):
    headers = {"Authorization": user_session["session_token"]}
    r = requests.get(user_session['url'] + f"payments/{user_session['username']}", headers=headers)
    assert r.status_code == 403

def test_get_payments_user_admin(user_session, admin_session):
    target_user = user_session['username']
    seed_body = {"transaction": uuid.uuid4().hex[:12], "amount": 10.99}

    user_headers = {"Authorization": user_session["session_token"]}
    seed_response = requests.post(user_session['url'] + 'payments', json=seed_body, headers=user_headers)
    assert seed_response.status_code == 201, f"Seeding failed: {seed_response.status_code} {seed_response.text}"

    admin_headers = {"Authorization": admin_session["session_token"]}
    get_response = requests.get(user_session['url'] + f'payments/{target_user}', headers=admin_headers)
    assert get_response.status_code == 200
    assert get_response.headers.get("Content-Type", "").startswith("application/json")

    items = get_response.json()
    assert isinstance(items, list), "GET /payments/{user} should return a JSON array"

    matches = [p for p in items if p.get("transaction") == seed_body["transaction"]]
    assert matches, f"Expected to find transaction {seed_body['transaction']} for user {target_user}"

    payment = matches[0]
    assert payment.get("initiator") == target_user
    assert payment.get("transaction") == seed_body["transaction"]
    assert "amount" in payment