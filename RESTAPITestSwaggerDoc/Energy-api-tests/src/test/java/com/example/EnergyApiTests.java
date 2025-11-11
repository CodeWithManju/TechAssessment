package com.example;

import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.junit.BeforeClass;
import org.junit.Test;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

import static io.restassured.RestAssured.*;
import static org.hamcrest.Matchers.*;

public class EnergyApiTests {

    private static final String BASE_URL = "https://qacandidatetest.ensek.io/ENSEK";
    private static final int DEFAULT_QUANTITY = 10;
    private static List<Integer> orderIds = new ArrayList<>();
    private static String firstOrderDate;

    @BeforeClass
    public static void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // Helper: Buy energy
    private int buyEnergy(int energyId, int quantity) {
        Response response = given()
                .pathParam("id", energyId)
                .pathParam("quantity", quantity)
                .when()
                .put("/buy/{id}/{quantity}")
                .then()
                .statusCode(200)
                .extract().response();

        return response.jsonPath().getInt("orderId");
    }

    // Helper: Get energy IDs
    private List<Integer> getEnergyIds() {
        Response response = given()
                .when()
                .get("/api/energy-ids")
                .then()
                .statusCode(200)
                .extract().response();

        List<Integer> ids = response.jsonPath().getList("id");
        if (ids == null || ids.isEmpty()) throw new IllegalStateException("No energy IDs retrieved.");
        return ids;
    }

    // Helper: Verify order details
    private void verifyOrderDetails(int orderId, int quantity) {
        given().pathParam("orderId", orderId)
                .when().get("/orders/{orderId}")
                .then().statusCode(200)
                .body("id", equalTo(orderId))
                .body("quantity", equalTo(quantity));
    }

    @Test
    public void testResetDataUnauthorized() {
        given().when().post("/reset")
                .then().statusCode(401);
    }

    @Test
    public void testBuyEnergyAndStoreOrders() {
        for (int id : getEnergyIds()) {
            int orderId = buyEnergy(id, DEFAULT_QUANTITY);
            orderIds.add(orderId);
        }
    }

    @Test
    public void testGetOrdersAndVerifyDetails() {
        Response ordersResponse = given()
                .when().get("/orders")
                .then().statusCode(200)
                .extract().response();

        List<String> creationDates = ordersResponse.jsonPath().getList("creationDate");
        if (!creationDates.isEmpty()) firstOrderDate = creationDates.get(0);

        for (int orderId : orderIds) {
            verifyOrderDetails(orderId, DEFAULT_QUANTITY);
        }
    }

    @Test
    public void testCountOrdersBeforeNow() {
        if (firstOrderDate == null) throw new IllegalStateException("No order date available.");

        List<String> dates = given()
                .when().get("/orders")
                .then().statusCode(200)
                .extract().jsonPath().getList("creationDate");

        long count = dates.stream()
                .filter(d -> d.compareTo(LocalDateTime.now().toString()) < 0)
                .count();

        System.out.println("Orders before current time: " + count);
    }

    @Test
    public void testUnauthorizedLogin() {
        String payload = "{\"username\":\"wrongUser\",\"password\":\"wrongPassword\"}";
        given().contentType(ContentType.JSON).body(payload)
                .when().post("/login")
                .then().statusCode(401)
                .body("message", equalTo("Unauthorized"));
    }

    @Test
    public void testBadRequestOnBuy() {
        given().pathParam("id", 1)
                .pathParam("quantity", -5)
                .when().put("/buy/{id}/{quantity}")
                .then().statusCode(400)
                .body("message", equalTo("Bad Request"));
    }
}
