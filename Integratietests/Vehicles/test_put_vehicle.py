import uuid
import requests
import pytest

VERIFY = False  # Zelf-ondertekend cert

# PUT /vehicle/{id}


def test_update_vehicle_success(login_as_user):
    # eerst vehicle aanmaken
    create_payload = {
        "licensePlate": f"UPD-{uuid.uuid4().hex[:6]}",
        "make": "Toyota",
        "model": "Yaris",
        "color": "Blue",
        "year": 2019,
    }
    headers = {"Authorization": login_as_user["session_token"]}
    res = requests.post(login_as_user["url"] + "vehicle",
                        json=create_payload, headers=headers, verify=VERIFY)
    vid = res.json()["id"]

    # update
    r = requests.put(
        login_as_user["url"] + f"vehicle/{vid}",
        json={"color": "Red"},
        headers=headers,
        verify=VERIFY
    )
    assert r.status_code == 200
    assert r.json()["color"] == "Red"


def test_update_vehicle_not_found(login_as_user):
    headers = {"Authorization": login_as_user["session_token"]}
    r = requests.put(
        login_as_user["url"] + "vehicle/999999",
        json={"color": "Green"},
        headers=headers,
        verify=VERIFY
    )
    assert r.status_code == 404


def test_update_vehicle_no_token(login_as_user):
    r = requests.put(login_as_user["url"] + "vehicle/1",
                     json={"color": "X"}, verify=VERIFY)
    assert r.status_code == 401
