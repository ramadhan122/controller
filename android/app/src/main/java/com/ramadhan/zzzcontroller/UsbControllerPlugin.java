package com.ramadhan.zzzcontroller;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbAccessory;
import android.hardware.usb.UsbManager;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.os.ParcelFileDescriptor;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.annotation.CapacitorPlugin;
import com.getcapacitor.PluginMethod;

import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;

@CapacitorPlugin(name = "UsbController")
public class UsbControllerPlugin extends Plugin {

    private static final String TAG = "ZZZ_USB";

    private static final String ACTION_USB_PERMISSION =
            "com.ramadhan.zzzcontroller.USB_PERMISSION";

    private UsbManager usbManager;
    private UsbAccessory accessory;

    private ParcelFileDescriptor fileDescriptor;
    private FileInputStream inputStream;
    private FileOutputStream outputStream;

    private PluginCall pendingCall;

    private final Handler handler =
            new Handler(Looper.getMainLooper());

    private boolean autoConnectRunning = false;

    // ========================================================
    // USB RECEIVER
    // ========================================================

    private final BroadcastReceiver usbPermissionReceiver =
            new BroadcastReceiver() {

                @Override
                public void onReceive(
                        Context context,
                        Intent intent) {

                    String action = intent.getAction();

                    android.util.Log.d(
                            TAG,
                            "Broadcast diterima: " + action
                    );

                    // ====================================================
                    // ACCESSORY ATTACHED
                    // ====================================================

                    if (UsbManager.ACTION_USB_ACCESSORY_ATTACHED.equals(action)) {

                        android.util.Log.d(
                                TAG,
                                "USB ACCESSORY ATTACHED"
                        );

                        startAutoConnect();

                        return;
                    }

                    // ====================================================
                    // ACCESSORY DETACHED
                    // ====================================================

                    if (UsbManager.ACTION_USB_ACCESSORY_DETACHED.equals(action)) {

                        android.util.Log.d(
                                TAG,
                                "USB ACCESSORY DETACHED"
                        );

                        closeAccessory();

                        return;
                    }

                    // ====================================================
                    // USB PERMISSION
                    // ====================================================

                    if (!ACTION_USB_PERMISSION.equals(action)) {
                        return;
                    }

                    boolean granted =
                            intent.getBooleanExtra(
                                    UsbManager.EXTRA_PERMISSION_GRANTED,
                                    false
                            );

                    android.util.Log.d(
                            TAG,
                            "USB permission result: " + granted
                    );

                    if (!granted) {

                        if (pendingCall != null) {

                            pendingCall.reject(
                                    "Izin USB Accessory ditolak"
                            );

                            pendingCall = null;
                        }

                        return;
                    }

                    boolean opened = openAccessory();

                    if (!opened) {

                        if (pendingCall != null) {

                            pendingCall.reject(
                                    "Gagal membuka USB Accessory"
                            );

                            pendingCall = null;
                        }

                        return;
                    }

                    android.util.Log.d(
                            TAG,
                            "USB Accessory berhasil terhubung"
                    );

                    if (pendingCall != null) {

                        JSObject result =
                                new JSObject();

                        result.put(
                                "connected",
                                true
                        );

                        result.put(
                                "manufacturer",
                                accessory.getManufacturer()
                        );

                        result.put(
                                "model",
                                accessory.getModel()
                        );

                        pendingCall.resolve(result);

                        pendingCall = null;
                    }
                }
            };

    // ========================================================
    // LOAD
    // ========================================================

    @Override
    public void load() {

        usbManager =
                (UsbManager) getContext()
                        .getSystemService(
                                Context.USB_SERVICE
                        );

        IntentFilter filter =
                new IntentFilter();

        filter.addAction(
                ACTION_USB_PERMISSION
        );

        filter.addAction(
                UsbManager.ACTION_USB_ACCESSORY_ATTACHED
        );

        filter.addAction(
                UsbManager.ACTION_USB_ACCESSORY_DETACHED
        );

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {

            getContext().registerReceiver(
                    usbPermissionReceiver,
                    filter,
                    Context.RECEIVER_EXPORTED
            );

        } else {

            getContext().registerReceiver(
                    usbPermissionReceiver,
                    filter
            );
        }

        android.util.Log.d(
                TAG,
                "USB BroadcastReceiver terdaftar"
        );

        // Mulai pengecekan accessory.
        startAutoConnect();
    }

    // ========================================================
    // AUTO CONNECT LOOP
    // ========================================================

    private void startAutoConnect() {

        if (autoConnectRunning) {
            return;
        }

        autoConnectRunning = true;

        android.util.Log.d(
                TAG,
                "AUTO CONNECT LOOP dimulai"
        );

        autoConnectCheck();
    }

    private void autoConnectCheck() {

        if (!autoConnectRunning) {
            return;
        }

        try {

            UsbAccessory[] accessories =
                    usbManager.getAccessoryList();

            if (accessories != null &&
                    accessories.length > 0) {

                accessory = accessories[0];

                android.util.Log.d(
                        TAG,
                        "Accessory ditemukan: " +
                                accessory.getManufacturer() +
                                " / " +
                                accessory.getModel()
                );

                if (outputStream != null) {

                    android.util.Log.d(
                            TAG,
                            "Accessory sudah terbuka"
                    );

                } else if (usbManager.hasPermission(accessory)) {

                    android.util.Log.d(
                            TAG,
                            "Permission sudah ada, membuka accessory"
                    );

                    boolean opened =
                            openAccessory();

                    if (opened) {

                        android.util.Log.d(
                                TAG,
                                "AUTO CONNECT BERHASIL"
                        );

                    } else {

                        android.util.Log.e(
                                TAG,
                                "AUTO CONNECT GAGAL membuka accessory"
                        );
                    }

                } else {

                    android.util.Log.d(
                            TAG,
                            "Permission belum ada, meminta permission"
                    );

                    requestUsbPermission();
                }

            } else {

                android.util.Log.d(
                        TAG,
                        "Belum ada USB Accessory"
                );
            }

        } catch (Exception e) {

            android.util.Log.e(
                    TAG,
                    "AUTO CONNECT ERROR",
                    e
            );
        }

        // Cek lagi 1 detik kemudian.
        handler.postDelayed(
                this::autoConnectCheck,
                1000
        );
    }

    // ========================================================
    // REQUEST USB PERMISSION
    // ========================================================

    private void requestUsbPermission() {

        try {

            if (accessory == null) {
                return;
            }

            Intent permissionIntent =
                    new Intent(
                            ACTION_USB_PERMISSION
                    );

            permissionIntent.setPackage(
                    getContext().getPackageName()
            );

            int flags =
                    PendingIntent.FLAG_UPDATE_CURRENT;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {

                flags |=
                        PendingIntent.FLAG_IMMUTABLE;
            }

            PendingIntent pendingIntent =
                    PendingIntent.getBroadcast(
                            getContext(),
                            0,
                            permissionIntent,
                            flags
                    );

            pendingCall = null;

            android.util.Log.d(
                    TAG,
                    "requestPermission() auto connect"
            );

            usbManager.requestPermission(
                    accessory,
                    pendingIntent
            );

        } catch (Exception e) {

            android.util.Log.e(
                    TAG,
                    "REQUEST PERMISSION ERROR",
                    e
            );
        }
    }

    // ========================================================
    // CONNECT
    // ========================================================

    @PluginMethod
    public void connect(PluginCall call) {

        try {

            UsbAccessory[] accessories =
                    usbManager.getAccessoryList();

            if (accessories == null ||
                    accessories.length == 0) {

                call.reject(
                        "USB Accessory tidak ditemukan"
                );

                return;
            }

            accessory = accessories[0];

            android.util.Log.d(
                    TAG,
                    "Accessory ditemukan: " +
                            accessory.getManufacturer() +
                            " / " +
                            accessory.getModel()
            );

            if (usbManager.hasPermission(accessory)) {

                if (openAccessory()) {

                    JSObject result =
                            new JSObject();

                    result.put(
                            "connected",
                            true
                    );

                    result.put(
                            "manufacturer",
                            accessory.getManufacturer()
                    );

                    result.put(
                            "model",
                            accessory.getModel()
                    );

                    call.resolve(result);

                } else {

                    call.reject(
                            "Gagal membuka USB Accessory"
                    );
                }

                return;
            }

            android.util.Log.d(
                    TAG,
                    "Meminta USB permission..."
            );

            pendingCall = call;

            Intent permissionIntent =
                    new Intent(
                            ACTION_USB_PERMISSION
                    );

            permissionIntent.setPackage(
                    getContext().getPackageName()
            );

            int flags =
                    PendingIntent.FLAG_UPDATE_CURRENT;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {

                flags |=
                        PendingIntent.FLAG_IMMUTABLE;
            }

            PendingIntent pendingIntent =
                    PendingIntent.getBroadcast(
                            getContext(),
                            0,
                            permissionIntent,
                            flags
                    );

            usbManager.requestPermission(
                    accessory,
                    pendingIntent
            );

        } catch (Exception e) {

            android.util.Log.e(
                    TAG,
                    "CONNECT ERROR",
                    e
            );

            call.reject(
                    "Connect error: " +
                            e.getMessage()
            );
        }
    }

    // ========================================================
    // OPEN ACCESSORY
    // ========================================================

    private boolean openAccessory() {

        android.util.Log.d(
                TAG,
                "openAccessory() dipanggil"
        );

        try {

            if (accessory == null) {

                android.util.Log.e(
                        TAG,
                        "Accessory NULL"
                );

                return false;
            }

            closeAccessory();

            ParcelFileDescriptor fd =
                    usbManager.openAccessory(
                            accessory
                    );

            if (fd == null) {

                android.util.Log.e(
                        TAG,
                        "openAccessory() NULL"
                );

                return false;
            }

            fileDescriptor = fd;

            inputStream =
                    new FileInputStream(
                            fd.getFileDescriptor()
                    );

            outputStream =
                    new FileOutputStream(
                            fd.getFileDescriptor()
                    );

            android.util.Log.d(
                    TAG,
                    "USB Accessory BERHASIL dibuka"
            );

            return true;

        } catch (SecurityException e) {

            android.util.Log.e(
                    TAG,
                    "SECURITY EXCEPTION",
                    e
            );

            closeAccessory();

            return false;

        } catch (Exception e) {

            android.util.Log.e(
                    TAG,
                    "OPEN ACCESSORY ERROR",
                    e
            );

            closeAccessory();

            return false;
        }
    }

    // ========================================================
    // SEND
    // ========================================================

    @PluginMethod
    public void send(PluginCall call) {

        if (outputStream == null) {

            call.reject(
                    "USB belum terhubung"
            );

            return;
        }

        String data =
                call.getString("data");

        if (data == null) {

            call.reject(
                    "Data kosong"
            );

            return;
        }

        try {

            outputStream.write(
                    data.getBytes("UTF-8")
            );

            outputStream.flush();

            call.resolve();

        } catch (IOException e) {

            android.util.Log.e(
                    TAG,
                    "SEND ERROR",
                    e
            );

            closeAccessory();

            call.reject(
                    "Gagal mengirim data: " +
                            e.getMessage()
            );
        }
    }

    // ========================================================
    // DISCONNECT
    // ========================================================

    @PluginMethod
    public void disconnect(PluginCall call) {

        closeAccessory();

        call.resolve();
    }

    // ========================================================
    // CLOSE
    // ========================================================

    private void closeAccessory() {

        try {

            if (inputStream != null) {
                inputStream.close();
            }

        } catch (Exception ignored) {
        }

        try {

            if (outputStream != null) {
                outputStream.close();
            }

        } catch (Exception ignored) {
        }

        try {

            if (fileDescriptor != null) {
                fileDescriptor.close();
            }

        } catch (Exception ignored) {
        }

        inputStream = null;
        outputStream = null;
        fileDescriptor = null;
    }

    // ========================================================
    // DESTROY
    // ========================================================

    @Override
    protected void handleOnDestroy() {

        autoConnectRunning = false;

        handler.removeCallbacksAndMessages(null);

        closeAccessory();

        try {

            getContext().unregisterReceiver(
                    usbPermissionReceiver
            );

        } catch (Exception ignored) {
        }

        super.handleOnDestroy();
    }
}