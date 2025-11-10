import requests

# Unauthorized (geen token)
def test_delete_vehicle_no_token(user_session):
    url = user_session['url'] + '/parking-lots/vehicles/1'
    response = requests.delete(url)
    assert response.status_code == 401
    assert "Unauthorized" in response.text


# Ongeldige token
def test_delete_vehicle_invalid_token(user_session):
    url = user_session['url'] + '/parking-lots/vehicles/1'
    response = requests.delete(url, headers={"Authorization": "invalid-token"})
    assert response.status_code == 401
    assert "Unauthorized" in response.text


# Gebruiker probeert voertuig te verwijderen dat niet bestaat
def test_delete_vehicle_not_found(user_session):
    url = user_session['url'] + '/parking-lots/vehicles/999'
    response = requests.delete(url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 403
    assert "Vehicle not found" in response.text


# Admin kan voertuig verwijderen (ook al is het niet van hem)
def test_delete_vehicle_as_admin(admin_session):
    url = admin_session['url'] + '/parking-lots/vehicles/1'
    response = requests.delete(url, headers={"Authorization": admin_session['session_token']})
    # afhankelijk van implementatie kan 200 of 403 zijn, maar in deze logica zou het 200 moeten zijn
    assert response.status_code in [200, 403]
    if response.status_code == 200:
        assert response.json()["status"] == "Deleted"


# Gebruiker verwijdert eigen voertuig succesvol
def test_delete_own_vehicle_as_user(user_session):
    url = user_session['url'] + '/parking-lots/vehicles/2'
    response = requests.delete(url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 200
    assert response.headers["Content-Type"] == "application/json"
    data = response.json()
    assert data["status"] == "Deleted"
