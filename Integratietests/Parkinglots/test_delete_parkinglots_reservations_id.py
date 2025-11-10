import requests

# Unauthorized (geen token)
def test_delete_reservation_no_token(admin_session):
    url = admin_session['url'] + '/parking-lots/reservations/1'
    response = requests.delete(url)
    assert response.status_code == 401
    assert "Unauthorized" in response.text


# Ongeldige token
def test_delete_reservation_invalid_token(admin_session):
    url = admin_session['url'] + '/parking-lots/reservations/1'
    response = requests.delete(url, headers={"Authorization": "invalid-token"})
    assert response.status_code == 401
    assert "Unauthorized" in response.text


# Reservation niet gevonden
def test_delete_reservation_not_found(admin_session):
    url = admin_session['url'] + '/parking-lots/reservations/9999'
    response = requests.delete(url, headers={"Authorization": admin_session['session_token']})
    assert response.status_code == 404
    assert "Reservation not found" in response.text


# User probeert andermans reservering te verwijderen → verboden
def test_delete_reservation_access_denied_for_user(user_session):
    url = user_session['url'] + '/parking-lots/reservations/1'  # Reservation van iemand anders
    response = requests.delete(url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 403
    assert "Access denied" in response.text


# Admin verwijdert reservering succesvol
def test_delete_reservation_as_admin(admin_session):
    url = admin_session['url'] + '/parking-lots/reservations/2'  # Bestaande reservering
    response = requests.delete(url, headers={"Authorization": admin_session['session_token']})
    assert response.status_code == 200
    assert response.headers["Content-Type"] == "application/json"
    data = response.json()
    assert data["status"] == "Deleted"
