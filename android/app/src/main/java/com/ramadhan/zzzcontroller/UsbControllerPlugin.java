package com.ramadhan.zzzcontroller;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbAccessory;
import android.hardware.usb.UsbManager;
import android.os.Build;
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

    private static final String ACTION_USB_PERMISSION =
            "com.ramadhan.zzzcontroller.USB_PERMISSION";

    private UsbManager usbManager;
    private UsbAccessory accessory;
    private ParcelFileDescriptor fileDescriptor;
    private FileInputStream inputStream;
    private FileOutputStream outputStream;

    private PluginCall pendingCall;

    private final BroadcastReceiver usbPermissionReceiver =
        new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {

                android.util.Log.d(
                        "ZZZ_USB",
                        "Broadcast diterima: " +
                        intent.getAction()
                );

                if (!ACTION_USB_PERMISSION.equals(intent.getAction())) {
                    return;
                }

                boolean granted = intent.getBooleanExtra(
                        UsbManager.EXTRA_PERMISSION_GRANTED,
                        false
                );

                android.util.Log.d(
                        "ZZZ_USB",
                        "USB permission result: " + granted
                );

                if (!granted) {

                    if (pendingCall != null) {
                        pendingCall.reject(
                                "Izin USB Accessory ditolak"
                        );
                        pendingCall = null;
                    } else {
                        android.util.Log.e(
                                "ZZZ_USB",
                                "Izin USB ditolak saat auto connect"
                        );
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
                        "ZZZ_USB",
                        "USB Accessory berhasil terhubung"
                );

                if (pendingCall != null) {

                    JSObject result = new JSObject();

                    result.put("connected", true);
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

    @Override
    public void load() {

        usbManager = (UsbManager) getContext()
                .getSystemService(Context.USB_SERVICE);

        IntentFilter filter =
                new IntentFilter(ACTION_USB_PERMISSION);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {

            getContext().registerReceiver(
                    usbPermissionReceiver,
                    filter,
                    Context.RECEIVER_NOT_EXPORTED
            );

        } else {

            getContext().registerReceiver(
                    usbPermissionReceiver,
                    filter
            );
        }

        // Coba konek otomatis saat aplikasi dibuka
        getActivity().runOnUiThread(() -> {

            try {

                UsbAccessory[] accessories =
                        usbManager.getAccessoryList();

                if (accessories != null &&
                        accessories.length > 0) {

                    android.util.Log.d(
                            "ZZZ_USB",
                            "Accessory terdeteksi saat aplikasi dibuka"
                    );

                    autoConnect();

                } else {

                    android.util.Log.d(
                            "ZZZ_USB",
                            "Belum ada Accessory saat aplikasi dibuka"
                    );
                }

            } catch (Exception e) {

                android.util.Log.e(
                        "ZZZ_USB",
                        "AUTO CONNECT ERROR",
                        e
                );
            }
        });
    }

    private void autoConnect() {

        try {

            UsbAccessory[] accessories =
                    usbManager.getAccessoryList();

            if (accessories == null ||
                    accessories.length == 0) {

                android.util.Log.d(
                        "ZZZ_USB",
                        "Auto connect: Accessory tidak ditemukan"
                );

                return;
            }

            accessory = accessories[0];

            android.util.Log.d(
                    "ZZZ_USB",
                    "Auto connect: Accessory ditemukan: " +
                    accessory.getManufacturer() +
                    " / " +
                    accessory.getModel()
            );

            if (usbManager.hasPermission(accessory)) {

                android.util.Log.d(
                        "ZZZ_USB",
                        "Auto connect: permission sudah ada"
                );

                boolean opened = openAccessory();

                if (opened) {

                    android.util.Log.d(
                            "ZZZ_USB",
                            "Auto connect: USB berhasil dibuka"
                    );

                } else {

                    android.util.Log.e(
                            "ZZZ_USB",
                            "Auto connect: gagal membuka USB"
                    );
                }

                return;
            }

            android.util.Log.d(
                    "ZZZ_USB",
                    "Auto connect: permission belum ada"
            );

            Intent permissionIntent =
                    new Intent(ACTION_USB_PERMISSION);

            permissionIntent.setPackage(
                    getContext().getPackageName()
            );

            int flags =
                    PendingIntent.FLAG_UPDATE_CURRENT;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                flags |= PendingIntent.FLAG_IMMUTABLE;
            }

            PendingIntent pendingIntent =
                    PendingIntent.getBroadcast(
                            getContext(),
                            0,
                            permissionIntent,
                            flags
                    );

            // Tidak menggunakan pendingCall karena ini auto connect
            pendingCall = null;

            usbManager.requestPermission(
                    accessory,
                    pendingIntent
            );

        } catch (Exception e) {

            android.util.Log.e(
                    "ZZZ_USB",
                    "AUTO CONNECT ERROR",
                    e
            );
        }
    }

    @PluginMethod
    public void connect(PluginCall call) {

        try {

            UsbAccessory[] accessories =
                    usbManager.getAccessoryList();

            if (accessories == null ||
                    accessories.length == 0) {

                android.util.Log.e(
                        "ZZZ_USB",
                        "USB Accessory tidak ditemukan"
                );

                call.reject(
                        "USB Accessory tidak ditemukan"
                );

                return;
            }

            accessory = accessories[0];

            android.util.Log.d(
                    "ZZZ_USB",
                    "Accessory ditemukan: " +
                    accessory.getManufacturer() +
                    " / " +
                    accessory.getModel()
            );

            if (usbManager.hasPermission(accessory)) {

                android.util.Log.d(
                        "ZZZ_USB",
                        "USB permission SUDAH ada"
                );

                if (openAccessory()) {

                    JSObject result = new JSObject();

                    result.put("connected", true);
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
                    "ZZZ_USB",
                    "Meminta USB permission..."
            );

            pendingCall = call;

            Intent permissionIntent =
                    new Intent(ACTION_USB_PERMISSION);

            permissionIntent.setPackage(
                    getContext().getPackageName()
            );

            int flags =
                    PendingIntent.FLAG_UPDATE_CURRENT;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                flags |= PendingIntent.FLAG_IMMUTABLE;
            }

            PendingIntent pendingIntent =
                    PendingIntent.getBroadcast(
                            getContext(),
                            0,
                            permissionIntent,
                            flags
                    );

            android.util.Log.d(
                    "ZZZ_USB",
                    "requestPermission() dipanggil"
            );

            usbManager.requestPermission(
                    accessory,
                    pendingIntent
            );

        } catch (Exception e) {

            android.util.Log.e(
                    "ZZZ_USB",
                    "CONNECT ERROR",
                    e
            );

            call.reject(
                    "Connect error: " +
                    e.getMessage()
            );
        }
    }

    private boolean openAccessory() {

        android.util.Log.d(
                "ZZZ_USB",
                "openAccessory() dipanggil"
        );

        try {

            android.util.Log.d(
                    "ZZZ_USB",
                    "Opening accessory: " +
                    accessory.getManufacturer() +
                    " / " +
                    accessory.getModel()
            );

            ParcelFileDescriptor fd =
                    usbManager.openAccessory(accessory);

            if (fd == null) {

                android.util.Log.e(
                        "ZZZ_USB",
                        "openAccessory() mengembalikan NULL"
                );

                return false;
            }

            android.util.Log.d(
                    "ZZZ_USB",
                    "USB Accessory BERHASIL dibuka"
            );

            fileDescriptor = fd;

            inputStream = new FileInputStream(
                    fd.getFileDescriptor()
            );

            outputStream = new FileOutputStream(
                    fd.getFileDescriptor()
            );

            return true;

        } catch (SecurityException e) {

            android.util.Log.e(
                    "ZZZ_USB",
                    "SECURITY EXCEPTION",
                    e
            );

            return false;

        } catch (Exception e) {

            android.util.Log.e(
                    "ZZZ_USB",
                    "OPEN ACCESSORY ERROR",
                    e
            );

            return false;
        }
    }

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

            call.reject(
                    "Gagal mengirim data: " +
                    e.getMessage()
            );
        }
    }

    @PluginMethod
    public void disconnect(PluginCall call) {

        try {

            if (inputStream != null) {
                inputStream.close();
            }

            if (outputStream != null) {
                outputStream.close();
            }

            if (fileDescriptor != null) {
                fileDescriptor.close();
            }

            inputStream = null;
            outputStream = null;
            fileDescriptor = null;
            accessory = null;

            call.resolve();

        } catch (Exception e) {

            call.reject(
                    "Gagal disconnect: " +
                    e.getMessage()
            );
        }
    }

    @Override
    protected void handleOnDestroy() {

        try {

            getContext().unregisterReceiver(
                    usbPermissionReceiver
            );

        } catch (Exception ignored) {
        }

        super.handleOnDestroy();
    }
}