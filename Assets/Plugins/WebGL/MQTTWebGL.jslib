mergeInto(LibraryManager.library, {
  MQTT_Connect: function (urlPtr, clientIdPtr) {
    const url = UTF8ToString(urlPtr);
    const clientId = UTF8ToString(clientIdPtr);

    if (typeof mqtt === "undefined") {
      console.error("[MQTT] mqtt.js is not loaded.");
      return;
    }

    if (window.unityMqttClient && window.unityMqttClient.connected) {
      console.log("[MQTT] Already connected.");
      return;
    }

    window.unityMqttClient = mqtt.connect(url, {
      clientId: clientId,
      clean: true,
      reconnectPeriod: 2000,
      connectTimeout: 5000
    });

    window.unityMqttClient.on("connect", function () {
      console.log("[MQTT] Connected:", url);
    });

    window.unityMqttClient.on("reconnect", function () {
      console.log("[MQTT] Reconnecting...");
    });

    window.unityMqttClient.on("error", function (err) {
      console.error("[MQTT] Error:", err);
    });

    window.unityMqttClient.on("close", function () {
      console.log("[MQTT] Closed");
    });
  },

  MQTT_Publish: function (topicPtr, payloadPtr) {
    const topic = UTF8ToString(topicPtr);
    const payload = UTF8ToString(payloadPtr);

    if (!window.unityMqttClient || !window.unityMqttClient.connected) {
      console.warn("[MQTT] Publish skipped. Client not connected.");
      return;
    }

    window.unityMqttClient.publish(topic, payload);
    console.log("[MQTT] Published:", topic, payload);
  },

  MQTT_Disconnect: function () {
    if (window.unityMqttClient) {
      window.unityMqttClient.end();
      window.unityMqttClient = null;
      console.log("[MQTT] Disconnected");
    }
  }
});