# import uuid
# import requests


# def test_post_payments_refund_requires_auth(user_session):
#     url = user_session['url'] + 'payments/refund'
#     r = requests.post(url, json={"amount": 5.0})
#     assert r.status_code == 401


# def test_post_payments_refund_requires_admin(user_session):
#     url = user_session['url'] + 'payments/refund'
#     headers = {"Authorization": user_session["session_token"]}
#     r = requests.post(url, json={"amount": 5.0}, headers=headers)
#     assert r.status_code == 403


# def test_post_payments_refund_missing_amount_returns_400(admin_session):
#     url = admin_session['url'] + 'payments/refund'
#     headers = {"Authorization": admin_session["session_token"]}
#     r = requests.post(url, json={}, headers=headers)
#     assert r.status_code == 400


# def test_post_payments_refund_amount_must_be_number_returns_400(admin_session):
#     url = admin_session['url'] + 'payments/refund'
#     headers = {"Authorization": admin_session["session_token"]}
#     body = {"amount": "not-a-number"}
#     r = requests.post(url, json=body, headers=headers)
#     assert r.status_code == 400


# def test_post_payments_refund_admin_creates_negative_payment(admin_session):
#     base = admin_session['url']
#     headers = {"Authorization": admin_session["session_token"]}
#     coupled_to = "tx-" + uuid.uuid4().hex[:10]
#     body = {"amount": 12.34, "coupled_to": coupled_to}

#     r = requests.post(base + 'payments/refund', json=body, headers=headers)
#     assert r.status_code == 201, f"{r.status_code} {r.text}"
#     assert r.headers.get("Content-Type", "").startswith("application/json")

#     data = r.json()
#     assert data.get("status") == "Success"
#     payment = data.get("payment")
#     assert isinstance(payment, dict)
#     assert payment.get("amount") == -abs(body["amount"])
#     assert "transaction" in payment
#     assert "hash" in payment and payment["hash"]
#     assert payment.get("completed") is False
#     assert payment.get("processed_by") == admin_session["username"]
