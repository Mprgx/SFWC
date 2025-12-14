import requests
import pytest

VERIFY = False  # Zelf-ondertekend cert

# DELETE /vehicle/{id}


def test_delete_vehicle_success(login_as_user):
    create_payload = {
        "licensePlate": "DEL-001",
        "make": "Audi",
        "model": "A1",
        "color": "White",
        "year": 2016,
    }
    headers = {"Authorization": login_as_user["session_token"]}
    res = requests.post(login_as_user["url"] + "vehicle",
                        json=create_payload, headers=headers, verify=VERIFY)
    vid = res.json()["id"]

    r = requests.delete(
        login_as_user["url"] + f"vehicle/{vid}", headers=headers, verify=VERIFY)
    assert r.status_code == 200


def test_delete_vehicle_not_found(login_as_user):
    headers = {"Authorization": login_as_user["session_token"]}
    r = requests.delete(
        login_as_user["url"] + "vehicle/999999", headers=headers, verify=VERIFY)
    assert r.status_code == 404


def test_delete_vehicle_no_token(login_as_user):
    r = requests.delete(login_as_user["url"] + "vehicle/1", verify=VERIFY)
    assert r.status_code == 401
