<template>
  <view class="page page-container">
    <u-loading-page :loading="loading" loading-text="加载中..." />

    <view v-if="!loading" class="card">
      <u-form label-position="top">
        <u-form-item label="角色编码" required>
          <u-input
            v-model="form.roleCode"
            placeholder="如 admin"
            :disabled="isEdit"
            clearable />
        </u-form-item>
        <u-form-item label="角色名称" required>
          <u-input v-model="form.roleName" placeholder="如 管理员" clearable />
        </u-form-item>
        <u-form-item label="描述">
          <u-textarea v-model="form.description" placeholder="可选" count maxlength="200" />
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
import { reactive, ref } from 'vue';
import { onLoad } from '@dcloudio/uni-app';
import * as roleService from '@/service/role.service';
import { useUserStore } from '@/store/user';

const userStore = useUserStore();

const isEdit = ref(false);
const roleId = ref(null);
const loading = ref(false);
const saving = ref(false);

const form = reactive({
  roleCode: '',
  roleName: '',
  description: '',
  isActive: true
});

onLoad(options => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
    return;
  }

  if (options.id) {
    isEdit.value = true;
    roleId.value = Number(options.id);
    if (!userStore.hasPermission('system.roles:edit')) {
      uni.showToast({ title: '无权编辑角色', icon: 'none' });
      setTimeout(goBack, 500);
      return;
    }
    uni.setNavigationBarTitle({ title: '编辑角色' });
    loadRole();
  } else {
    if (!userStore.hasPermission('system.roles:create')) {
      uni.showToast({ title: '无权新建角色', icon: 'none' });
      setTimeout(goBack, 500);
      return;
    }
    uni.setNavigationBarTitle({ title: '新建角色' });
  }
});

async function loadRole() {
  loading.value = true;
  try {
    const role = await roleService.getRoleById(roleId.value);
    form.roleCode = role.roleCode;
    form.roleName = role.roleName;
    form.description = role.description || '';
    form.isActive = role.isActive;
  } catch (err) {
    uni.showToast({ title: err.message || '加载失败', icon: 'none' });
    setTimeout(goBack, 500);
  } finally {
    loading.value = false;
  }
}

async function submit() {
  const roleCode = form.roleCode.trim();
  const roleName = form.roleName.trim();
  const description = form.description.trim();

  if (!isEdit.value && !roleCode) {
    uni.showToast({ title: '请输入角色编码', icon: 'none' });
    return;
  }
  if (!roleName) {
    uni.showToast({ title: '请输入角色名称', icon: 'none' });
    return;
  }

  saving.value = true;
  try {
    if (isEdit.value) {
      await roleService.updateRole(roleId.value, {
        roleName,
        description: description || undefined,
        isActive: form.isActive
      });
    } else {
      await roleService.createRole({
        roleCode,
        roleName,
        description: description || undefined
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
.actions {
  margin-top: 32rpx;
}
</style>
