<template>
  <view class="page page-container">
    <u-loading-page :loading="loading" loading-text="加载中..." />

    <view v-if="!loading" class="card">
      <view class="avatar-section" @click="chooseAvatar">
        <u-avatar :src="avatarPreview" :text="avatarText" size="72" bg-color="#1677ff" />
        <text class="avatar-section__tip muted">{{ uploadingAvatar ? '上传中...' : '点击上传头像' }}</text>
      </view>

      <u-form label-position="top">
        <u-form-item label="用户名" required>
          <u-input
            v-model="form.userName"
            placeholder="请输入用户名"
            :disabled="isEdit"
            clearable />
        </u-form-item>
        <u-form-item :label="isEdit ? '密码' : '密码'" :required="!isEdit">
          <u-input
            v-model="form.password"
            type="password"
            :placeholder="isEdit ? '留空则不修改' : '至少 6 位'"
            clearable />
        </u-form-item>
        <u-form-item label="显示名">
          <u-input v-model="form.displayName" placeholder="可选" clearable />
        </u-form-item>
        <u-form-item label="邮箱">
          <u-input v-model="form.email" placeholder="可选" clearable />
        </u-form-item>
        <u-form-item v-if="isEdit" label="启用">
          <u-switch v-model="form.isActive" />
        </u-form-item>
      </u-form>
    </view>

    <view class="actions">
      <u-button type="primary" :loading="saving" text="保存" @click="submit" />
      <u-button text="取消" custom-style="margin-top: 20rpx;" @click="goBack" />
    </view>
  </view>
</template>

<script setup>
import { computed, reactive, ref } from 'vue';
import { onLoad } from '@dcloudio/uni-app';
import * as userService from '@/service/user.service';
import { useUserStore } from '@/store/user';
import { resolveAssetUrl } from '@/utils/format';

const userStore = useUserStore();

const isEdit = ref(false);
const userId = ref(null);
const loading = ref(false);
const saving = ref(false);
const uploadingAvatar = ref(false);
const avatarUrl = ref('');

const form = reactive({
  userName: '',
  password: '',
  displayName: '',
  email: '',
  isActive: true
});

const avatarPreview = computed(() => resolveAssetUrl(avatarUrl.value));
const avatarText = computed(() => (form.userName || 'U').slice(0, 1).toUpperCase());

onLoad(options => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
    return;
  }

  if (options.id) {
    isEdit.value = true;
    userId.value = Number(options.id);
    if (!userStore.hasPermission('system.users:edit')) {
      uni.showToast({ title: '无权编辑用户', icon: 'none' });
      setTimeout(goBack, 500);
      return;
    }
    uni.setNavigationBarTitle({ title: '编辑用户' });
    loadUser();
  } else {
    if (!userStore.hasPermission('system.users:create')) {
      uni.showToast({ title: '无权新建用户', icon: 'none' });
      setTimeout(goBack, 500);
      return;
    }
    uni.setNavigationBarTitle({ title: '新建用户' });
  }
});

async function loadUser() {
  loading.value = true;
  try {
    const user = await userService.getUserById(userId.value);
    form.userName = user.userName;
    form.displayName = user.displayName || '';
    form.email = user.email || '';
    form.isActive = user.isActive;
    form.password = '';
    avatarUrl.value = user.avatarUrl || '';
  } catch (err) {
    uni.showToast({ title: err.message || '加载失败', icon: 'none' });
    setTimeout(goBack, 500);
  } finally {
    loading.value = false;
  }
}

function chooseAvatar() {
  uni.chooseImage({
    count: 1,
    sizeType: ['compressed'],
    success: async res => {
      const filePath = res.tempFilePaths[0];
      uploadingAvatar.value = true;
      try {
        avatarUrl.value = await userService.uploadAvatar(filePath);
        uni.showToast({ title: '头像上传成功', icon: 'success' });
      } catch (err) {
        uni.showToast({ title: err.message || '头像上传失败', icon: 'none' });
      } finally {
        uploadingAvatar.value = false;
      }
    }
  });
}

async function submit() {
  const userName = form.userName.trim();
  const password = form.password.trim();
  const displayName = form.displayName.trim();
  const email = form.email.trim();

  if (!isEdit.value && !userName) {
    uni.showToast({ title: '请输入用户名', icon: 'none' });
    return;
  }
  if (!isEdit.value && password.length < 6) {
    uni.showToast({ title: '密码至少 6 位', icon: 'none' });
    return;
  }
  if (isEdit.value && password && password.length < 6) {
    uni.showToast({ title: '密码至少 6 位', icon: 'none' });
    return;
  }

  saving.value = true;
  try {
    if (isEdit.value) {
      await userService.updateUser(userId.value, {
        displayName: displayName || undefined,
        email: email || undefined,
        isActive: form.isActive,
        password: password || undefined,
        avatarUrl: avatarUrl.value || undefined
      });
    } else {
      await userService.createUser({
        userName,
        password,
        displayName: displayName || undefined,
        email: email || undefined,
        avatarUrl: avatarUrl.value || undefined
      });
    }
    uni.showToast({ title: isEdit.value ? '更新成功' : '创建成功', icon: 'success' });
    setTimeout(goBack, 400);
  } catch (err) {
    uni.showToast({ title: err.message || '保存失败', icon: 'none' });
  } finally {
    saving.value = false;
  }
}

function goBack() {
  uni.navigateBack();
}
</script>

<style scoped lang="scss">
.avatar-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16rpx 0 32rpx;

  &__tip {
    margin-top: 16rpx;
    font-size: 24rpx;
  }
}

.actions {
  margin-top: 32rpx;
}
</style>
